#!/usr/bin/env python3
"""Run dotnet test w/ coverage. Print missed lines + partial branches."""
import glob
import subprocess
import sys
import xml.etree.ElementTree as ET


def main():
    if len(sys.argv) < 2:
        print("usage: find_uncovered.py <path/to/Tests.csproj>")
        sys.exit(1)

    csproj = sys.argv[1]
    subprocess.run(
        ["dotnet", "test", csproj, "--collect:XPlat Code Coverage"],
        check=False,
    )

    files = glob.glob("**/TestResults/*/coverage.cobertura.xml", recursive=True)
    if not files:
        print("no coverage file found")
        sys.exit(1)

    latest = max(files, key=lambda f: f)
    tree = ET.parse(latest)
    root = tree.getroot()

    print(f"line-rate={root.get('line-rate')} branch-rate={root.get('branch-rate')}")

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


if __name__ == "__main__":
    main()
