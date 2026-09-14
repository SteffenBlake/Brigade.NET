#!/usr/bin/env python3
"""Run dotnet test w/ coverage. Print missed lines + partial branches."""
from pathlib import Path
from collections import defaultdict
import json
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET


def main():
    if len(sys.argv) < 2:
        print("usage: find_uncovered.py <path/to/Tests.csproj>")
        sys.exit(1)

    csproj = Path(sys.argv[1]).resolve()
    results = csproj.parent / "TestResults" / ("coverage-" + uuid.uuid4().hex)
    result = subprocess.run(
        ["dotnet", "test", str(csproj), "--collect:XPlat Code Coverage", "--results-directory", str(results),
         "--", "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura"],
        check=False,
    )

    if result.returncode:
        sys.exit(result.returncode)
    files = list(results.glob("**/coverage.cobertura.xml"))
    if not files:
        print("no coverage file found")
        sys.exit(1)

    latest = max(files, key=lambda f: f.stat().st_mtime)
    tree = ET.parse(latest)
    root = tree.getroot()

    print(f"line-rate={root.get('line-rate')} branch-rate={root.get('branch-rate')}")
    print(f"report={latest}")
    for package in root.findall("./packages/package"):
        print(f"{package.get('name')}: line-rate={package.get('line-rate')} branch-rate={package.get('branch-rate')}")

    for cls in root.iter("class"):
        lines = cls.find("lines")
        if lines is None:
            continue
        missed = [line_element.get("number") for line_element in lines if line_element.get("hits") == "0"]
        partial = [
            (line_element.get("number"), line_element.get("condition-coverage"))
            for line_element in lines
            if line_element.get("condition-coverage") and not line_element.get("condition-coverage").startswith("100%")
        ]
        if missed or partial:
            print(f"{cls.get('filename')} :: {cls.get('name')}")
            if missed:
                print(f"  missed lines: {missed}")
            if partial:
                print(f"  partial branches: {partial}")


def merge_reports(paths):
    """Union exact sequence/branch points from fresh reports of the same build."""
    modules = defaultdict(lambda: {"lines": {}, "branches": {}})
    for path in paths:
        with open(path) as stream:
            report = json.load(stream)
        for module, documents in report.items():
            points = modules[module]
            for filename, classes in documents.items():
                for cls, methods in classes.items():
                    for method, data in methods.items():
                        for line, hits in data["Lines"].items():
                            key = (filename, cls, method, int(line))
                            points["lines"][key] = points["lines"].get(key, False) or hits > 0
                        for branch in data["Branches"]:
                            key = (filename, cls, method, branch["Line"], branch["Offset"], branch["EndOffset"], branch["Path"])
                            points["branches"][key] = points["branches"].get(key, False) or branch["Hits"] > 0
    for module, points in sorted(modules.items()):
        rates = []
        for kind, values in points.items():
            rates.append(f"{kind}={100 * sum(values.values()) / len(values):.2f}% ({sum(values.values())}/{len(values)})" if values else f"{kind}=N/A")
        print(module + ": " + " ".join(rates))
        missed = defaultdict(lambda: {"lines": set(), "branches": set()})
        for kind, values in points.items():
            for key, covered in values.items():
                if not covered:
                    missed[key[0]][kind].add(key[3])
        for filename, gaps in sorted(missed.items()):
            print(f"  {filename}: missed lines={sorted(gaps['lines'])}; partial branch lines={sorted(gaps['branches'])}")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--merge":
        merge_reports(sys.argv[2:])
    else:
        main()
