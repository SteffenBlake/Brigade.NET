"""Run PR tests and report a gate on merged Source line and branch coverage."""

import argparse
from collections import defaultdict
from html import escape
import json
import os
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
RESULTS = ROOT / "artifacts/pr-checks"
THRESHOLD = 95
APPHOST_SUITE = "Example.IntegrationTests"
HINT = "See .agents/skills/writing-unit-tests/SKILL.md and use its find_uncovered.py script to locate missed lines and branches."
TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def test_projects():
    return sorted((ROOT / "Tests").rglob("*.csproj"))


def test():
    if RESULTS.exists():
        shutil.rmtree(RESULTS)
    RESULTS.mkdir(parents=True)
    executions = []
    for project in test_projects():
        suite = project.parent.name
        output = RESULTS / suite
        args = ["dotnet", "test", str(project), "-c", "Release", "--no-build", "--no-restore",
                "--logger", "trx", "--results-directory", str(output), "--verbosity", "minimal"]
        if suite != APPHOST_SUITE:
            args += ["--collect", "XPlat Code Coverage", "--settings", str(ROOT / "eng/coverage.runsettings")]
        print(f"Running {suite}", flush=True)
        result = subprocess.run(args, cwd=ROOT)
        executions.append({"suite": suite, "exit_code": result.returncode, "coverage": suite != APPHOST_SUITE})
        # Persist each result so an interrupted run cannot silently look complete.
        (RESULTS / "executions.json").write_text(json.dumps(executions, indent=2) + "\n")
    return int(any(item["exit_code"] for item in executions))


def source_path(filename):
    normalized = filename.replace("\\", "/")
    # Coverlet may resolve the PDB path to the checkout or retain the CI path map.
    if "/Source/" in normalized:
        relative = "Source/" + normalized.split("/Source/", 1)[1]
    elif normalized.startswith("Source/"):
        relative = normalized
    else:
        return None
    if {"obj", "bin", ".generated"}.intersection(Path(relative).parts):
        return None
    return relative


def merge(paths):
    modules = defaultdict(lambda: {"lines": {}, "branches": {}})
    for path in paths:
        for module, documents in json.loads(path.read_text()).items():
            for filename, classes in documents.items():
                filename = source_path(filename)
                if filename is None:
                    continue
                points = modules[Path(module).name]
                for cls, methods in classes.items():
                    for method, data in methods.items():
                        for line, hits in data["Lines"].items():
                            key = (filename, cls, method, int(line))
                            points["lines"][key] = points["lines"].get(key, False) or hits > 0
                        for branch in data["Branches"]:
                            key = (filename, cls, method, branch["Line"], branch["Offset"], branch["EndOffset"], branch["Path"])
                            points["branches"][key] = points["branches"].get(key, False) or branch["Hits"] > 0
    return dict(modules)


def count(points, kind):
    return sum(points[kind].values()), len(points[kind])


def rate(covered, total):
    return f"{100 * covered / total:.2f}% ({covered}/{total})" if total else "N/A"


def passes(covered, total):
    # Compare exact counts; rounding 94.999% to 95.00% must not pass the gate.
    return total > 0 and covered * 100 >= THRESHOLD * total


def write_badge(totals, valid=True):
    covered, total = totals["lines"]
    percentage = f"{100 * covered / total:.2f}%" if valid and total else "N/A"
    color = "#9f9f9f" if not valid or not total else "#4c1" if passes(covered, total) else "#e05d44"
    tooltip = escape(f"Line coverage: {rate(*totals['lines'])}; branch coverage: {rate(*totals['branches'])}" if valid
                     else "Coverage unavailable: build or coverage collection is incomplete.")
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="136" height="20" role="img" aria-label="coverage: {percentage}">
<title>{tooltip}</title>
<linearGradient id="shade" x2="0" y2="100%"><stop offset="0" stop-color="#bbb" stop-opacity=".1"/><stop offset="1" stop-opacity=".1"/></linearGradient>
<clipPath id="round"><rect width="136" height="20" rx="3"/></clipPath>
<g clip-path="url(#round)"><rect width="72" height="20" fill="#555"/><rect x="72" width="64" height="20" fill="{color}"/><rect width="136" height="20" fill="url(#shade)"/></g>
<g fill="#fff" text-anchor="middle" font-family="Verdana,DejaVu Sans,sans-serif" font-size="11">
<text x="36" y="15" fill="#010101" fill-opacity=".3">coverage</text><text x="36" y="14">coverage</text>
<text x="104" y="15" fill="#010101" fill-opacity=".3">{percentage}</text><text x="104" y="14">{percentage}</text>
</g></svg>\n'''
    (RESULTS / "coverage.svg").write_text(svg)


def summarize_tests(executions):
    rows = []
    errors = []
    for item in executions:
        paths = list((RESULTS / item["suite"]).glob("*.trx"))
        if len(paths) != 1:
            errors.append(f"Missing or ambiguous test report: {item['suite']}")
            rows.append(f"| {item['suite']} | Failed: report unavailable | — | — | — |")
            continue
        root = ET.parse(paths[0]).getroot()
        counters = root.find("t:ResultSummary/t:Counters", TRX_NS)
        if counters is None:
            errors.append(f"Missing test counts: {item['suite']}")
            continue
        total = int(counters.get("total", "0"))
        passed = int(counters.get("passed", "0"))
        failed = int(counters.get("failed", "0"))
        skipped = total - int(counters.get("executed", "0"))
        success = item["exit_code"] == 0 and failed == 0 and total > 0
        if not success:
            errors.append(f"Tests failed or did not run: {item['suite']}")
        rows.append(f"| {item['suite']} | {'Passed' if success else 'Failed'} | {passed} | {failed} | {skipped} |")
    return rows, errors


def report(build_outcome, report_only=False):
    RESULTS.mkdir(parents=True, exist_ok=True)
    execution_file = RESULTS / "executions.json"
    executions = json.loads(execution_file.read_text()) if execution_file.exists() else []
    expected = {project.parent.name for project in test_projects()}
    errors = []
    if build_outcome != "success":
        errors.append(f"Build did not succeed ({build_outcome}); coverage cannot be validated.")
    if {item["suite"] for item in executions} != expected or len(executions) != len(expected):
        errors.append("Test run is incomplete; every test suite must run before coverage can pass.")
    coverage_valid = not errors
    test_rows, test_errors = summarize_tests(executions)
    errors += test_errors
    reports = []
    for item in executions:
        if not item["coverage"]:
            continue
        # VSTest also copies attachments into its nested In directory; use originals.
        found = list((RESULTS / item["suite"]).glob("*/coverage.json"))
        if len(found) != 1:
            errors.append(f"Missing or ambiguous coverage report: {item['suite']}")
            coverage_valid = False
        reports += found
    modules = merge(reports)
    totals = {kind: tuple(sum(count(points, kind)[i] for points in modules.values()) for i in (0, 1))
              for kind in ("lines", "branches")}
    write_badge(totals, coverage_valid and bool(modules))
    for kind, (covered, total) in totals.items():
        if not report_only and not passes(covered, total):
            errors.append(f"Source {kind} coverage is {rate(covered, total)}; at least {THRESHOLD}% is required. {HINT}")
    if not modules:
        errors.append("No Source coverage was collected.")
    coverage_rows = []
    gaps = []
    for module, points in sorted(modules.items()):
        coverage_rows.append(f"| {module} | {rate(*count(points, 'lines'))} | {rate(*count(points, 'branches'))} |")
        for kind in ("lines", "branches"):
            for point, covered in sorted(points[kind].items()):
                if not covered:
                    gaps.append(f"{point[0]}:{point[3]} ({kind}, {module})")
    text = ["## Build and tests", "", f"Build: **{build_outcome}**", "",
            "| Suite | Result | Passed | Failed | Skipped |", "|---|---|---:|---:|---:|", *test_rows,
            "", "## Source coverage", "", f"Required for PRs: at least {THRESHOLD}% for both line and branch coverage.",
            "Fresh reports from this build are merged by exact sequence and branch points; percentages are not averaged.",
            "Aspire AppHost coverage and generated files are excluded. The gate applies to the combined Source totals.", "",
            "| Assembly | Lines | Branches |", "|---|---:|---:|", *coverage_rows,
            f"| **Total** | **{rate(*totals['lines'])}** | **{rate(*totals['branches'])}** |", "",
            f"Mode: **{'report only' if report_only else 'PR gate'}**. {'Issues reported' if report_only and errors else 'FAILED' if errors else 'PASSED'}.",
            "", *[f"- {error}" for error in errors], "", HINT]
    summary = "\n".join(text) + "\n"
    print(summary)
    (RESULTS / "summary.txt").write_text(summary)
    (RESULTS / "uncovered.txt").write_text("\n".join(sorted(set(gaps))) + "\n")
    (RESULTS / "coverage-summary.json").write_text(json.dumps({"threshold": THRESHOLD, "totals": totals, "errors": errors}, indent=2) + "\n")
    (RESULTS / "report.html").write_text("<!doctype html><meta charset='utf-8'><title>PR build, tests and coverage</title>"
                                        "<style>body{font:15px monospace;max-width:1200px;margin:2rem auto}pre{white-space:pre-wrap}</style>"
                                        f"<h1>PR checks</h1><pre>{escape(summary)}</pre><h2>Uncovered points</h2><pre>{escape(chr(10).join(sorted(set(gaps))))}</pre>")
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with Path(os.environ["GITHUB_STEP_SUMMARY"]).open("a") as output:
            output.write(summary)
    for error in errors:
        # Escape GitHub workflow-command control characters.
        escaped = error.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
        print(f"::{'warning' if report_only else 'error'}::{escaped}")
    return int(bool(errors) and not report_only)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["test", "report"])
    parser.add_argument("--build-outcome", default="success")
    parser.add_argument("--report-only", action="store_true")
    args = parser.parse_args()
    raise SystemExit(test() if args.command == "test" else report(args.build_outcome, args.report_only))
