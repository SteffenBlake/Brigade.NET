#!/usr/bin/env python3
"""Run existing parity tests and benchmarks, then generate charts and the README.

Requires .NET 10, Docker, k6, Python 3.11+, and matplotlib==3.10.7.
Run: python3 Benchmarks/run.py
Render saved data: python3 Benchmarks/run.py --render-only RUN_DIRECTORY
Raw outputs: Benchmarks/.work/runs/ (ignored by Git).
Public outputs: Benchmarks/Charts/*.png and Benchmarks/README.md.
"""


from dataclasses import dataclass
from datetime import datetime, timezone
from itertools import product
from pathlib import Path
from string import Template
from uuid import uuid4
import argparse
import csv
import hashlib
import importlib.util
import json
import math
import os
import platform
import random
import re
import shutil
import statistics
import subprocess
import sys
import xml.etree.ElementTree as ET


# INVENTORY



DATABASES = ("sqlserver", "postgresql", "mysql", "mariadb", "sqlite")
DATABASE_LABELS = ("SQL Server", "PostgreSQL", "MySQL", "MariaDB", "SQLite")
STACKS = ("brigade", "fluent-ef-mediatr")
STACK_LABELS = ("Brigade", "FluentEfMediatr")
OPERATIONS = ("invalid", "create", "search")


@dataclass(frozen=True)
class DotnetBenchmark:
    key: str
    project: str
    benchmark_type: str
    parameter: str | None
    cases: tuple[str, ...]
    methods: tuple[str, ...]
    labels: tuple[str, ...]


BDN_SUITES = (
    DotnetBenchmark(
        "database-querying",
        "DatabaseQuerying/Tests",
        "PointLookupBenchmarks",
        "Database",
        DATABASES,
        ("MiseAsync", "DapperAsync", "EfCoreAsync"),
        ("Mise", "Dapper", "EF Core"),
    ),
    DotnetBenchmark(
        "query-compilation",
        "DatabaseQuerying/Compilation",
        "QueryCompilationBenchmarks",
        None,
        ("",),
        ("MiseCompile", "EfCoreCached", "EfCoreUncached"),
        ("Mise", "EF Core: cached", "EF Core: cache bypass"),
    ),
    DotnetBenchmark(
        "validation",
        "Validation/Tests",
        "ValidationBenchmarks",
        "Scenario",
        ("AllValid", "SomeValid", "NonValid"),
        ("Expo", "Validly", "FluentValidation", "DataAnnotations"),
        ("Expo", "Validly", "FluentValidation", "DataAnnotations"),
    ),
)

API_PROJECT = "Api/Runner"
STARTUP_PROJECT = "Startup/Runner"

CHART_NAMES = (
    "database-querying.png",
    "query-compilation.png",
    "validation.png",
    "api-invalid.png",
    "api-create.png",
    "api-search.png",
    "startup.png",
)


# EXECUTION



class CommandRunner:
    def __init__(self, repository: Path, directory: Path):
        self.repository = repository
        self.directory = directory
        self.directory.mkdir(parents=True, exist_ok=True)

    def run(
        self,
        label: str,
        arguments: list[str],
        environment: dict[str, str] | None = None,
        allowed_exit_codes: tuple[int, ...] | None = (0,),
    ) -> int:
        print(f"\n[{label}]", flush=True)
        with (self.directory / f"{label}.log").open("w", encoding="utf-8") as log:
            with subprocess.Popen(
                arguments,
                cwd=self.repository,
                env=os.environ | (environment or {}),
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                bufsize=1,
            ) as process:
                try:
                    assert process.stdout is not None
                    for line in process.stdout:
                        print(line, end="", flush=True)
                        log.write(line)
                    code = process.wait()
                except BaseException:
                    process.terminate()
                    try:
                        process.wait(timeout=15)
                    except subprocess.TimeoutExpired:
                        process.kill()
                    raise

        if allowed_exit_codes is not None and code not in allowed_exit_codes:
            raise RuntimeError(f"{label} exited with {code}. See {self.directory / (label + '.log')}")
        return code


# METADATA



def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def command(arguments: list[str], repository: Path) -> str:
    return subprocess.check_output(arguments, cwd=repository, text=True, stderr=subprocess.STDOUT).strip()


def source_digest(repository: Path) -> str:
    output = subprocess.check_output(
        ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], cwd=repository
    )
    digest = hashlib.sha256()
    for name in sorted(set(output.decode().split("\0")) - {""}):
        if name == "Benchmarks/README.md" or name.startswith("Benchmarks/Charts/"):
            continue
        path = repository / name
        if path.is_file():
            digest.update(name.encode())
            digest.update(path.read_bytes())
    return digest.hexdigest()


def read_optional(path: str) -> str:
    file = Path(path)
    return file.read_text().strip() if file.is_file() else "unavailable"


def capture(repository: Path, k6: str) -> dict:
    cpu = "unavailable"
    cpu_info = read_optional("/proc/cpuinfo")
    for line in cpu_info.splitlines():
        if line.startswith("model name"):
            cpu = line.split(":", 1)[1].strip()
            break
    memory_gib = None
    for line in read_optional("/proc/meminfo").splitlines():
        if line.startswith("MemTotal:"):
            memory_gib = int(line.split()[1]) / 1024**2

    return {
        "schemaVersion": 1,
        "startedUtc": utc_now(),
        "commit": command(["git", "rev-parse", "HEAD"], repository),
        "workingTreeDirty": bool(command(["git", "status", "--porcelain"], repository)),
        "sourceDigest": source_digest(repository),
        "machine": {
            "os": platform.platform(),
            "architecture": platform.machine(),
            "cpu": cpu,
            "logicalProcessors": os.cpu_count(),
            "availableProcessors": len(os.sched_getaffinity(0)) if hasattr(os, "sched_getaffinity") else None,
            "memoryGiB": memory_gib,
            "cgroupCpuMax": read_optional("/sys/fs/cgroup/cpu.max"),
            "cgroupMemoryMax": read_optional("/sys/fs/cgroup/memory.max"),
            "dotnetSdk": command(["dotnet", "--version"], repository),
            "dotnetRuntimes": command(["dotnet", "--list-runtimes"], repository),
            "docker": command(["docker", "version", "--format", "{{.Server.Version}}"], repository),
            "k6": command([k6, "version"], repository),
            "python": platform.python_version(),
        },
        "bdnLaunches": 3,
        "parityProjects": [],
        "completedBenchmarks": [],
    }


# RESULTS




@dataclass(frozen=True)
class BdnResult:
    case: str
    method: str
    mean_us: float
    sd_us: float
    allocated_bytes: float
    iterations: int


@dataclass(frozen=True)
class ApiResult:
    database: str
    stack: str
    operation: str
    repeat: int
    average_ms: float
    p95_ms: float
    p99_ms: float
    successful_rps: float
    successful_percent: float
    dropped_percent: float
    error_percent: float
    status: str


def number(value, name: str, minimum: float = 0) -> float:
    result = float(value)
    if not math.isfinite(result) or result < minimum:
        raise ValueError(f"Invalid {name}: {value!r}")
    return result


def only_file(root: Path, pattern: str) -> Path:
    paths = list(root.rglob(pattern))
    if len(paths) != 1:
        raise ValueError(f"Expected one {pattern} under {root}; found {len(paths)}")
    return paths[0]


def load_bdn(root: Path, suite: DotnetBenchmark, launches: int) -> list[BdnResult]:
    file = only_file(root, f"*{suite.benchmark_type}-report-full*.json")
    report = json.loads(file.read_text())
    results = []
    coverage = set()
    for row in report["Benchmarks"]:
        method = row["Method"]
        parameters = row.get("Parameters", "")
        case = ""
        if suite.parameter:
            match = re.search(rf"(?:^|,\s*){suite.parameter}=([^,]+)", parameters)
            if not match:
                raise ValueError(f"Missing {suite.parameter} in {parameters!r}")
            case = match.group(1).strip().strip('"')
        key = (case, method)
        if key in coverage:
            raise ValueError(f"Duplicate benchmark: {key}")
        coverage.add(key)
        stats = row.get("Statistics")
        if not stats or int(stats["N"]) < 5:
            raise ValueError(f"Missing or insufficient timed measurements for {key}")
        measured_launches = {
            int(item["LaunchIndex"])
            for item in row["Measurements"]
            if item["IterationMode"] == "Workload" and item["IterationStage"] == "Result"
        }
        if measured_launches != set(range(1, launches + 1)):
            raise ValueError(f"Incomplete process launches for {key}: {measured_launches}")
        results.append(BdnResult(
            case,
            method,
            number(stats["Mean"], "mean", 1e-12) / 1000,
            number(stats["StandardDeviation"], "standard deviation") / 1000,
            number(row["Memory"]["BytesAllocatedPerOperation"], "allocated bytes"),
            int(stats["N"]),
        ))
    expected = set(product(suite.cases, suite.methods))
    if coverage != expected:
        raise ValueError(f"Wrong {suite.key} coverage. Missing {expected - coverage}; unexpected {coverage - expected}")
    return results


def metric(summary: dict, name: str, value: str) -> float:
    return number(summary["metrics"][name]["values"][value], f"{name}.{value}")


def load_api(root: Path, commit: str) -> tuple[list[ApiResult], dict]:
    file = only_file(root, "results.csv")
    settings = json.loads((file.parent / "settings.json").read_text())
    if settings["Commit"] != commit:
        raise ValueError("API results and run metadata have different commits")
    if not ((file.parent / "complete.txt").exists() or (file.parent / "invalid.txt").exists()):
        raise ValueError("API run did not finish all measurements")
    repeats = int(settings["Repeats"])
    duration = number(settings["DurationSeconds"], "API duration", 1)
    rate = number(settings["Rate"], "API rate", 1)
    if repeats < 3:
        raise ValueError("The report requires at least three API repeats per case")
    results = []
    coverage = set()
    for row in csv.DictReader(file.open()):
        key = (row["database"], row["stack"], row["operation"], int(row["repeat"]))
        if key in coverage:
            raise ValueError(f"Duplicate API measurement: {key}")
        coverage.add(key)
        if row["status"] not in {"completed", "generator-limit", "threshold-breached"}:
            raise ValueError(f"API infrastructure failure: {key}: {row['status']}")
        if number(row["offeredRps"], "offered RPS") != rate:
            raise ValueError("API offered rate differs from settings")
        summary_name = row["summary"]
        if Path(summary_name).name != summary_name:
            raise ValueError("Summary must be a filename within its run directory")
        summary = json.loads((file.parent / summary_name).read_text())
        elapsed = number(summary["state"]["testRunDurationMs"], "k6 duration", 1) / 1000
        passed = metric(summary, "checks", "passes")
        sent = metric(summary, "http_reqs", "count")
        if passed > sent:
            raise ValueError("Expected one response check per request")
        scheduled = rate * duration
        if passed > scheduled + max(2, scheduled * 0.0001):
            raise ValueError("Successful request count exceeds the configured schedule")
        results.append(ApiResult(
            *key,
            metric(summary, "http_req_duration", "avg"),
            metric(summary, "http_req_duration", "p(95)"),
            metric(summary, "http_req_duration", "p(99)"),
            passed / elapsed,
            100 * passed / scheduled,
            100 * metric(summary, "dropped_iterations", "count") / scheduled,
            100 * metric(summary, "http_req_failed", "rate"),
            row["status"],
        ))
    expected = set(product(DATABASES, STACKS, OPERATIONS, range(repeats)))
    if coverage != expected:
        raise ValueError(f"API coverage incomplete: {len(coverage)} of {len(expected)} measurements")
    return results, settings


def load_startup(root: Path, commit: str) -> tuple[list[dict], dict]:
    file = only_file(root, "results.csv")
    settings = json.loads((file.parent / "settings.json").read_text())
    if settings["Commit"] != commit or not (file.parent / "complete.txt").exists():
        raise ValueError("Startup run is incomplete or has a different commit")
    repeats = int(settings["Repeats"])
    if repeats < 30:
        raise ValueError("The report requires at least thirty startup trials per stack")
    results = []
    coverage = set()
    for row in csv.DictReader(file.open()):
        key = (row["stack"], int(row["repeat"]))
        if key in coverage:
            raise ValueError(f"Duplicate startup measurement: {key}")
        coverage.add(key)
        results.append({
            "stack": row["stack"],
            "repeat": int(row["repeat"]),
            **{name: number(row[name], name, 1e-12) for name in (
                "launchToReadyMs", "entryToReadyMs", "launchToHealthMs"
            )},
        })
    if coverage != set(product(STACKS, range(1, repeats + 1))):
        raise ValueError("Startup trial coverage is incomplete")
    return results, settings


def load_all(run: Path) -> dict:
    manifest = json.loads((run / "run.json").read_text())
    expected = {suite.key for suite in BDN_SUITES} | {"api", "startup"}
    if manifest.get("schemaVersion") != 1 or set(manifest["completedBenchmarks"]) != expected:
        raise ValueError("A complete five-suite run manifest is required")
    if not manifest.get("finishedUtc") or not manifest.get("releaseBuildPassed") or not manifest["parityProjects"]:
        raise ValueError("Missing completed parity/build gates or run timestamp")
    data = {"manifest": manifest}
    for suite in BDN_SUITES:
        data[suite.key] = load_bdn(run / suite.key, suite, manifest["bdnLaunches"])
    data["api"], data["apiSettings"] = load_api(run / "api", manifest["commit"])
    data["startup"], data["startupSettings"] = load_startup(run / "startup", manifest["commit"])
    return data


# CHARTS





COLORS = ("#2563eb", "#d97706", "#059669", "#9333ea")
def configure_plotting() -> None:
    global matplotlib, plt, Line2D
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        from matplotlib.lines import Line2D
    except ImportError as error:
        raise RuntimeError("Install matplotlib==3.10.7 in the Python environment running this script.") from error

    plt.rcParams.update({
        "font.family": "DejaVu Sans",
        "font.size": 10,
        "axes.spines.top": False,
        "axes.spines.right": False,
        "axes.edgecolor": "#cbd5e1",
        "axes.labelcolor": "#334155",
        "text.color": "#0f172a",
        "xtick.color": "#475569",
        "ytick.color": "#475569",
        "figure.facecolor": "white",
        "axes.facecolor": "white",
        "savefig.facecolor": "white",
    })


def prepare(ax, title: str, ylabel: str, labels: tuple[str, ...]) -> None:
    ax.set_title(title, loc="left", fontsize=12, pad=15, weight="semibold")
    ax.set_ylabel(ylabel)
    ax.set_xticks(range(len(labels)), labels)
    ax.set_xlim(-0.6, len(labels) - 0.4)
    ax.grid(axis="y", color="#e2e8f0", linewidth=0.8)
    ax.set_axisbelow(True)


def legend(fig, labels: tuple[str, ...]) -> None:
    handles = [Line2D([], [], color=COLORS[i], marker="o", linestyle="", label=label)
               for i, label in enumerate(labels)]
    fig.legend(handles=handles, loc="upper center", bbox_to_anchor=(0.5, 0.915),
               ncol=len(labels), frameon=False)


def save(fig, output: Path, title: str, note: str, manifest: dict) -> None:
    fig.suptitle(title, x=0.075, y=0.98, ha="left", fontsize=19, weight="semibold")
    fig.text(0.075, 0.04, note, fontsize=9, color="#475569")
    fig.text(0.075, 0.015,
             f"Completed {manifest['finishedUtc']}  |  commit {manifest['commit'][:12]}",
             fontsize=8, color="#64748b")
    output.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(output, dpi=180)
    plt.close(fig)


def label_point(ax, x: float, y: float, offset: int = 9) -> None:
    value = f"{y:,.0f}" if y >= 1000 else (f"{y:.3g}" if y < 1 else f"{y:.2f}".rstrip("0").rstrip("."))
    ax.annotate(value, (x, y), xytext=(0, offset), textcoords="offset points",
                ha="center", fontsize=8, color="#334155")


def bdn_chart(suite, rows, directory: Path, manifest: dict) -> None:
    fig, axes = plt.subplots(1, 2, figsize=(13, 6))
    fig.subplots_adjust(left=0.075, right=0.98, top=0.80, bottom=0.18, wspace=0.30)
    titles = {
        "database-querying": "Indexed point lookup: ORM × database",
        "query-compilation": "Complex query: SQL text generation",
        "validation": "Model validation: engine × input scenario",
    }
    labels = DATABASE_LABELS if suite.key == "database-querying" else (
        ("All valid", "Some valid", "Non valid") if suite.key == "validation" else ("Complex query",)
    )
    by_key = {(row.case, row.method): row for row in rows}
    use_log = all(row.mean_us > row.sd_us for row in rows)
    prepare(axes[0], "Time per operation · lower is better", "Microseconds / operation", labels)
    prepare(axes[1], "Managed allocation · lower is better", "Bytes / operation", labels)
    if use_log:
        axes[0].set_yscale("log")
        axes[0].set_ylabel("Microseconds / operation · logarithmic scale")
    width = 0.7 / len(suite.methods)
    for method_index, method in enumerate(suite.methods):
        for case_index, case in enumerate(suite.cases):
            row = by_key[case, method]
            x = case_index + (method_index - (len(suite.methods) - 1) / 2) * width
            axes[0].errorbar(x, row.mean_us, yerr=row.sd_us, fmt="o", capsize=4,
                             color=COLORS[method_index], markersize=7)
            axes[1].scatter(x, row.allocated_bytes, color=COLORS[method_index], s=45)
            label_point(axes[0], x, row.mean_us, 9 + 10 * (method_index % 2))
            label_point(axes[1], x, row.allocated_bytes, 9 + 10 * (method_index % 2))
    axes[1].set_ylim(0, max(1, max(row.allocated_bytes for row in rows) * 1.3))
    legend(fig, suite.labels)
    minimum = min(row.iterations for row in rows)
    maximum = max(row.iterations for row in rows)
    save(fig, directory / f"{suite.key}.png", titles[suite.key],
         f"Points: BenchmarkDotNet means. Whiskers: ±1 SD of iteration means. "
         f"Timed iterations/case: {minimum}–{maximum}; process launches/case: {manifest['bdnLaunches']}.\n"
         "Allocation includes managed allocations recorded by MemoryDiagnoser. Whiskers are descriptive spread, not confidence intervals.",
         manifest)


def api_chart(operation: str, rows, settings: dict, directory: Path, manifest: dict) -> None:
    fig, axes = plt.subplots(3, 2, figsize=(13, 12))
    fig.subplots_adjust(left=0.075, right=0.98, top=0.86, bottom=0.12, hspace=0.42, wspace=0.30)
    fields = (
        ("average_ms", "Mean response time · lower is better", "Milliseconds", True),
        ("p95_ms", "p95 response time · lower is better", "Milliseconds", True),
        ("p99_ms", "p99 response time · lower is better", "Milliseconds", True),
        ("successful_rps", "Successful response rate · higher is better", "Checked successful responses / second", False),
        ("successful_percent", "Successful completion · higher is better", "% of configured request schedule", False),
        ("dropped_percent", "Dropped starts · lower is better", "% of configured request schedule", False),
    )
    selected = [row for row in rows if row.operation == operation]
    for ax, (field, title, units, logarithmic) in zip(axes.flat, fields):
        prepare(ax, title, units, DATABASE_LABELS)
        if logarithmic and all(getattr(row, field) > 0 for row in selected):
            ax.set_yscale("log")
            ax.set_ylabel(units + " · logarithmic scale")
        for stack_index, stack in enumerate(STACKS):
            for database_index, database in enumerate(DATABASES):
                values = [getattr(row, field) for row in selected
                          if row.stack == stack and row.database == database]
                mean = statistics.mean(values)
                x = database_index + (stack_index - 0.5) * 0.26
                ax.errorbar(x, mean, yerr=[[mean - min(values)], [max(values) - mean]],
                            fmt="o", capsize=4, color=COLORS[stack_index], markersize=6)
                label_point(ax, x, mean, 9 + 12 * stack_index)
        if field.endswith("percent"):
            ax.set_ylim(0, 110)
        elif not logarithmic:
            ax.set_ylim(0, max(1, max(getattr(row, field) for row in selected) * 1.3))
    legend(fig, STACK_LABELS)
    names = {"invalid": "Invalid payload", "create": "Create", "search": "Search"}
    save(fig, directory / f"api-{operation}.png", f"HTTP API: {names[operation]}",
         f"Offered rate: {settings['Rate']:,} starts/s. Measurement: {settings['DurationSeconds']}s. "
         f"Warmup: {settings['WarmupSeconds']}s. Repeats/case: {settings['Repeats']}.\n"
         "Points: means across runs. Whiskers: observed run range. p95/p99 are means of run percentiles.\n"
         "Successful rates count passed response checks; elapsed time includes k6 drain. Latency includes failed responses that reached the API.",
         manifest)


def startup_chart(rows: list[dict], settings: dict, directory: Path, manifest: dict) -> None:
    fig, axes = plt.subplots(1, 3, figsize=(13, 6))
    fig.subplots_adjust(left=0.075, right=0.98, top=0.79, bottom=0.18, wspace=0.35)
    metrics = (
        ("launchToReadyMs", "Process launch → ready hook"),
        ("entryToReadyMs", "Managed entry → ready hook"),
        ("launchToHealthMs", "Process launch → health response"),
    )
    rng = random.Random(1)
    for ax, (field, title) in zip(axes, metrics):
        prepare(ax, title, "Milliseconds · lower is better", STACK_LABELS)
        values = [[row[field] for row in rows if row["stack"] == stack] for stack in STACKS]
        boxes = ax.boxplot(values, positions=[0, 1], widths=0.35, patch_artist=True,
                           whis=(0, 100), showfliers=False,
                           medianprops={"color": "#0f172a", "linewidth": 1.8})
        for index, (box, samples) in enumerate(zip(boxes["boxes"], values)):
            box.set_facecolor(COLORS[index])
            box.set_alpha(0.2)
            ax.scatter([index + rng.uniform(-0.1, 0.1) for _ in samples], samples,
                       color=COLORS[index], s=13, alpha=0.55)
            ax.annotate(f"median {statistics.median(samples):.1f}", (index, max(samples)),
                        xytext=(0, 12), textcoords="offset points", ha="center", fontsize=9)
        ax.set_xticks([0, 1], STACK_LABELS)
        ax.tick_params(axis="x", labelsize=9)
        ax.set_ylim(bottom=0, top=max(max(group) for group in values) * 1.18)
    save(fig, directory / "startup.png", "ASP.NET Core: fresh-process startup",
         f"Trials/stack: {settings['Repeats']}. Points: individual trials. Boxes: interquartile range; centre line: median; whiskers: observed range.\n"
         "Published Release apps; no database or containers. The health-response measurement also includes runner scheduling and HTTP request time.",
         manifest)


def render_charts(data: dict, directory: Path) -> None:
    for suite in BDN_SUITES:
        bdn_chart(suite, data[suite.key], directory, data["manifest"])
    for operation in ("invalid", "create", "search"):
        api_chart(operation, data["api"], data["apiSettings"], directory, data["manifest"])
    startup_chart(data["startup"], data["startupSettings"], directory, data["manifest"])
    if {path.name for path in directory.glob("*.png")} != set(CHART_NAMES):
        raise ValueError("Generated chart set differs from the expected inventory")


# REPORT





def template_values(data: dict) -> dict[str, str]:
    manifest = data["manifest"]
    machine = manifest["machine"]
    api = data["apiSettings"]
    startup = data["startupSettings"]
    failed = sum(row.status != "completed" for row in data["api"])
    values = {
        "bdn_launches": manifest["bdnLaunches"],
        "api_rate": f"{api['Rate']:,}",
        "api_duration": api["DurationSeconds"],
        "api_warmup": api["WarmupSeconds"],
        "api_repeats": api["Repeats"],
        "api_vus": api["Vus"],
        "api_max_vus": api["MaxVus"],
        "api_memory": api["MinimumAvailableGiB"],
        "api_p95": api["P95Ms"],
        "api_p99": api["P99Ms"],
        "api_criteria_status": f"{failed} of {len(data['api'])} measured runs exceeded at least one configured criterion",
        "startup_repeats": startup["Repeats"],
        "started_utc": manifest["startedUtc"],
        "finished_utc": manifest["finishedUtc"],
        "commit": manifest["commit"],
        "working_tree": "modified" if manifest["workingTreeDirty"] else "clean",
        "source_digest": manifest["sourceDigest"],
        "parity_count": len(manifest["parityProjects"]),
        "os": machine["os"],
        "architecture": machine["architecture"],
        "cpu": machine["cpu"],
        "logical_processors": machine["logicalProcessors"],
        "available_processors": machine["availableProcessors"],
        "memory_gib": f"{machine['memoryGiB']:.1f}" if machine["memoryGiB"] is not None else "unavailable",
        "cgroup_cpu": machine["cgroupCpuMax"],
        "cgroup_memory": machine["cgroupMemoryMax"],
        "dotnet_sdk": machine["dotnetSdk"],
        "dotnet_runtimes": machine["dotnetRuntimes"],
        "docker": machine["docker"],
        "k6": machine["k6"],
        "python": machine["python"],
        "matplotlib": matplotlib.__version__,
    }
    return {key: str(value) for key, value in values.items()}


def publish(repository: Path, run: Path) -> None:
    data = load_all(run)
    template = Template((repository / "Benchmarks" / "README.template.md").read_text())
    text = template.substitute(template_values(data))
    staging = run / "report"
    staging.mkdir(exist_ok=True)
    render_charts(data, staging)
    (staging / "README.md").write_text(text, encoding="utf-8")
    normalized = {
        key: [vars(row) for row in value] if isinstance(value, list) and value and hasattr(value[0], "__dataclass_fields__") else value
        for key, value in data.items()
    }
    (run / "normalized.json").write_text(json.dumps(normalized, indent=2), encoding="utf-8")

    # Generate and validate the full set before replacing any published image.
    destination = repository / "Benchmarks" / "Charts"
    destination.mkdir(exist_ok=True)
    for name in CHART_NAMES:
        temporary = destination / (name + ".tmp")
        temporary.write_bytes((staging / name).read_bytes())
        os.replace(temporary, destination / name)
    report = repository / "Benchmarks" / "README.md"
    temporary = report.with_suffix(".md.tmp")
    temporary.write_text(text, encoding="utf-8")
    os.replace(temporary, report)
    print(f"Updated {report} and {len(CHART_NAMES)} images in {destination}", flush=True)


# PIPELINE




def project_path(repository: Path, relative: str) -> Path:
    projects = list((repository / "Benchmarks" / relative).glob("*.csproj"))
    if len(projects) != 1:
        raise ValueError(f"Expected one project in {relative}")
    return projects[0]


def preflight(repository: Path) -> list[Path]:
    if importlib.util.find_spec("matplotlib") is None:
        raise RuntimeError("Install the plotting dependencies from Benchmarks/Scripts/requirements.txt first.")
    for executable in ("dotnet", "docker", "git", os.environ.get("BENCH_K6", "k6")):
        if shutil.which(executable) is None:
            raise RuntimeError(f"Missing executable: {executable}")
    subprocess.run(["docker", "info"], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
    output = subprocess.check_output(
        ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], cwd=repository
    )
    projects = sorted({name for name in output.decode().split("\0")
                       if name.endswith(".csproj") and (repository / name).is_file()})
    registered = {item.attrib["Path"] for item in ET.parse(repository / "Brigade.NET.slnx").iter("Project")}
    missing = set(projects) - registered
    if missing:
        raise RuntimeError(f"Projects missing from the root solution: {sorted(missing)}")
    parity = [repository / name for name in projects if "ParityTests" in name.split("/")]
    if not parity:
        raise RuntimeError("No parity test projects were found")
    return parity


def save_manifest(run: Path, manifest: dict) -> None:
    temporary = run / "run.json.tmp"
    temporary.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    temporary.replace(run / "run.json")


def validate_parity(directory: Path) -> None:
    file = only_file(directory, "*.trx")
    counters = ET.parse(file).find(".//{*}Counters")
    if counters is None:
        raise ValueError(f"No test counters in {file}")
    total = int(counters.attrib["total"])
    passed = int(counters.attrib["passed"])
    executed = int(counters.attrib["executed"])
    if total < 1 or passed != total or executed != total:
        raise ValueError(f"Parity requires all tests to pass without skips: {passed}/{total} passed")


def run_all(repository: Path) -> Path:
    parity = preflight(repository)
    run = repository / "Benchmarks" / ".work" / "runs" / (utc_now().replace(":", "") + "-" + uuid4().hex[:8])
    run.mkdir(parents=True)
    manifest = capture(repository, os.environ.get("BENCH_K6", "k6"))
    save_manifest(run, manifest)
    runner = CommandRunner(repository, run / "logs")
    print(f"Raw outputs: {run}", flush=True)

    for index, project in enumerate(parity, start=1):
        label = f"parity-{index}-{project.parent.parent.name}"
        runner.run(label, ["dotnet", "test", str(project), "-c", "Release",
                           "--logger", "trx", "--results-directory", str(run / "parity" / label)])
        validate_parity(run / "parity" / label)
        manifest["parityProjects"].append(str(project.relative_to(repository)))
        save_manifest(run, manifest)

    runner.run("release-build", ["dotnet", "build", "Brigade.NET.slnx", "-c", "Release"])
    manifest["releaseBuildPassed"] = True
    save_manifest(run, manifest)

    for suite in BDN_SUITES:
        runner.run(suite.key, [
            "dotnet", "run", "--project", str(project_path(repository, suite.project)),
            "-c", "Release", "--no-build", "--no-restore", "--",
            "--filter", f"*{suite.benchmark_type}*", "--exporters", "json",
            "--artifacts", str(run / suite.key), "--launchCount", str(manifest["bdnLaunches"]),
            "--stopOnFirstError",
        ])
        load_bdn(run / suite.key, suite, manifest["bdnLaunches"])
        manifest["completedBenchmarks"].append(suite.key)
        save_manifest(run, manifest)

    api_directory = run / "api"
    api_code = runner.run("api", [
        "dotnet", "run", "--project", str(project_path(repository, API_PROJECT)),
        "-c", "Release", "--no-build", "--no-restore",
    ], environment={"BENCH_RESULTS": str(api_directory), "GITHUB_SHA": manifest["commit"]},
        allowed_exit_codes=None)
    # A completed overloaded run is reportable. An infrastructure crash or partial run is not.
    _, api_settings = load_api(api_directory, manifest["commit"])
    if api_code != 0 and not list(api_directory.rglob("invalid.txt")):
        raise RuntimeError("API runner exited unexpectedly without a completed criteria-failure marker")
    manifest["apiSettings"] = api_settings
    manifest["completedBenchmarks"].append("api")
    save_manifest(run, manifest)

    runner.run("startup", [
        "dotnet", "run", "--project", str(project_path(repository, STARTUP_PROJECT)),
        "-c", "Release", "--no-build", "--no-restore",
    ], environment={"STARTUP_RESULTS": str(run / "startup")})
    load_startup(run / "startup", manifest["commit"])
    manifest["completedBenchmarks"].append("startup")

    if source_digest(repository) != manifest["sourceDigest"]:
        raise RuntimeError("Source files changed during the run. Raw results were kept; report publication stopped.")
    manifest["finishedUtc"] = utc_now()
    save_manifest(run, manifest)
    return run


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--render-only", type=Path, metavar="RUN_DIRECTORY",
                        help="Regenerate charts and README from one completed saved run; do not execute benchmarks.")
    args = parser.parse_args()
    configure_plotting()
    repository = Path(__file__).resolve().parent.parent
    run = args.render_only.resolve() if args.render_only else run_all(repository)
    publish(repository, run)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nInterrupted. Previously saved raw measurements remain on disk.", file=sys.stderr)
        sys.exit(130)
    except Exception as error:
        print(f"Benchmark run failed: {error}", file=sys.stderr)
        sys.exit(1)
