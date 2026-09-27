"""Build, validate and publish the Brigade.NET release manifest. Python stdlib only."""

import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parent.parent
PACKAGES = ROOT / "artifacts/packages"
NUGET = "https://api.nuget.org/v3/index.json"


def run(*args, cwd=ROOT, env=None):
    subprocess.run([str(arg) for arg in args], cwd=cwd, env=env, check=True)


def version(value):
    if not re.fullmatch(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[a-z0-9]+(?:[.-][a-z0-9]+)*)?", value):
        raise ValueError("Use a full version such as 1.0.0 or 1.1.0-rc.1; no v or library prefix.")
    if "-" in value:
        for identifier in value.split("-", 1)[1].split("."):
            if identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"):
                raise ValueError("Numeric prerelease identifiers must not have leading zeroes.")
    return value


def catalog():
    data = json.loads((ROOT / "eng/packages.json").read_text())
    names = [name for group in data["groups"].values() for name in group] + data["internal"]
    actual = {path.parent.name for path in (ROOT / "Source").rglob("*.csproj")}
    if len(names) != len(set(names)) or set(names) != actual:
        raise ValueError(f"Package manifest must classify each Source project once: {set(names) ^ actual}")
    return data


def project(name):
    return ROOT / "Source" / name / f"Brigade.Net.{name}.csproj"


def public_packages():
    return [(group, name) for group, names in catalog()["groups"].items() for name in names]


def release_event():
    event = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text())
    release = event["release"]
    value = version(release["tag_name"])
    if release["draft"] or release["prerelease"] != ("-" in value):
        raise ValueError("The GitHub prerelease checkbox must match the tag's prerelease suffix.")
    commit = subprocess.check_output(["git", "rev-parse", f"refs/tags/{value}^{{commit}}"], cwd=ROOT, text=True).strip()
    if commit != os.environ["GITHUB_SHA"]:
        raise ValueError("Release tag does not match the workflow commit.")
    with Path(os.environ["GITHUB_OUTPUT"]).open("a") as output:
        output.write(f"version={value}\n")
    print(f"Release {value} from {commit}")


def pack(value):
    catalog()
    if PACKAGES.exists():
        shutil.rmtree(PACKAGES)
    PACKAGES.mkdir(parents=True)
    for _, name in public_packages():
        run("dotnet", "pack", project(name), "-c", "Release", "-o", PACKAGES,
            f"-p:Version={value}", f"-p:PackageVersion={value}", "-p:ContinuousIntegrationBuild=true",
            "-p:NuGetAudit=true", "-warnaserror:NU5128", "--verbosity", "minimal")
    inspect(value)


def inspect(value):
    expected = {f"Brigade.Net.{name}.{value}.nupkg" for _, name in public_packages()}
    if {path.name for path in PACKAGES.glob("*.nupkg")} != expected:
        raise ValueError("Package output does not match the public package manifest.")
    records = []
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    for group, name in public_packages():
        path = PACKAGES / f"Brigade.Net.{name}.{value}.nupkg"
        with ZipFile(path) as archive:
            files = archive.namelist()
            spec = ET.fromstring(archive.read(f"Brigade.Net.{name}.nuspec"))
            # NuGet chooses the nuspec schema based on the features in the package.
            namespace = spec.tag.split("}")[0].strip("{")
            ns = {"n": namespace}
            metadata = spec.find("n:metadata", ns)
            if metadata.findtext("n:id", namespaces=ns) != f"Brigade.Net.{name}" or metadata.findtext("n:version", namespaces=ns) != value:
                raise ValueError(f"Incorrect package identity: {path.name}")
            repo = metadata.find("n:repository", ns)
            if repo is None or repo.get("commit") != commit:
                raise ValueError(f"Missing or incorrect source commit: {path.name}")
            if not {"LICENSE", "README.md"}.issubset(files):
                raise ValueError(f"Missing package license or README: {path.name}")
            is_analyzer = ".Engines." in name
            dll = f"Brigade.Net.{name}.dll"
            required = f"analyzers/dotnet/cs/{dll}" if is_analyzer else f"lib/net10.0/{dll}"
            if required not in files:
                raise ValueError(f"Missing assembly {required}")
            if name.startswith("Mise.Engines.") or name == "Partie.Engines.AspNetCore":
                support = "Mise" if name.startswith("Mise.") else "Partie"
                if f"analyzers/dotnet/cs/Brigade.Net.{support}.Generator.dll" not in files:
                    raise ValueError(f"Missing generator support assembly: {name}")
            dependencies = metadata.findall("n:dependencies/n:group/n:dependency", ns)
            if is_analyzer and dependencies:
                raise ValueError(f"Analyzer package leaks build dependencies: {name}")
            if not is_analyzer:
                references = ET.parse(project(name)).getroot().iter("ProjectReference")
                expected_dependencies = {Path(reference.get("Include").replace("\\", "/")).stem
                                         for reference in references if reference.get("PrivateAssets") != "all"}
                actual_dependencies = {dependency.get("id") for dependency in dependencies
                                       if dependency.get("id", "").startswith("Brigade.Net.")}
                if actual_dependencies != expected_dependencies:
                    raise ValueError(f"Package dependencies do not match project references: {name}")
            for dependency in dependencies:
                if dependency.get("id", "").startswith("Brigade.Net.") and dependency.get("version") != value:
                    raise ValueError(f"Internal dependency version mismatch: {name}")
        records.append({"id": f"Brigade.Net.{name}", "group": group, "file": path.name,
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    (PACKAGES / "release-manifest.json").write_text(json.dumps({"version": value, "commit": commit, "packages": records}, indent=2) + "\n")
    print(f"Validated {len(records)} public packages.")


def smoke(value):
    # Test the real example using NuGet packages, with no Source project references or shared cache.
    artifacts(value)
    with tempfile.TemporaryDirectory(prefix="brigade-packages-") as work:
        work = Path(work)
        example = work / "Example"
        shutil.copytree(ROOT / "Example", example, ignore=shutil.ignore_patterns("bin", "obj", ".generated"))
        for path in example.rglob("*.csproj"):
            tree = ET.parse(path)
            for group in tree.getroot().findall("ItemGroup"):
                for reference in list(group.findall("ProjectReference")):
                    include = reference.get("Include").replace("\\", "/")
                    if "/Source/" in include:
                        group.remove(reference)
                        ET.SubElement(group, "PackageReference", {"Include": Path(include).stem, "Version": f"[{value}]"})
            tree.write(path, encoding="utf-8", xml_declaration=True)
        config = work / "NuGet.Config"
        configuration = ET.Element("configuration")
        sources = ET.SubElement(configuration, "packageSources")
        ET.SubElement(sources, "clear")
        ET.SubElement(sources, "add", {"key": "release", "value": str(PACKAGES)})
        ET.SubElement(sources, "add", {"key": "nuget.org", "value": NUGET})
        mapping = ET.SubElement(configuration, "packageSourceMapping")
        local = ET.SubElement(mapping, "packageSource", {"key": "release"})
        ET.SubElement(local, "package", {"pattern": "Brigade.Net.*"})
        external = ET.SubElement(mapping, "packageSource", {"key": "nuget.org"})
        ET.SubElement(external, "package", {"pattern": "*"})
        ET.ElementTree(configuration).write(config, encoding="utf-8", xml_declaration=True)
        env = dict(os.environ, NUGET_PACKAGES=str(work / "cache"))
        web = example / "Web/Brigade.Net.Example.Web.csproj"
        run("dotnet", "restore", web, "--configfile", config, env=env)
        run("dotnet", "build", web, "-c", "Release", "--no-restore", env=env)
        tests = work / "Tests/Example.IntegrationTests"
        shutil.copytree(ROOT / "Tests/Example.IntegrationTests", tests, ignore=shutil.ignore_patterns("bin", "obj"))
        integration = tests / "Brigade.Net.Example.IntegrationTests.csproj"
        run("dotnet", "restore", integration, "--configfile", config, env=env)
        run("dotnet", "test", integration, "-c", "Release", "--no-restore", "--logger", "trx",
            "--results-directory", ROOT / "artifacts/test-results/PackagedExample", env=env)
    print("The example builds and runs against all packaged engines and both library integrations.")


def tests(value):
    for path in sorted((ROOT / "Tests").rglob("*.csproj")):
        run("dotnet", "test", path, "-c", "Release", f"-p:Version={value}",
            "--logger", "trx", "--results-directory", ROOT / "artifacts/test-results" / path.parent.name,
            "--verbosity", "minimal")


def artifacts(value):
    manifest = json.loads((PACKAGES / "release-manifest.json").read_text())
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    if manifest["version"] != value or manifest["commit"] != commit:
        raise ValueError("Artifact version or commit differs from this release.")
    expected = {(f"Brigade.Net.{name}", group) for group, name in public_packages()}
    if {(entry["id"], entry["group"]) for entry in manifest["packages"]} != expected or len(manifest["packages"]) != len(expected):
        raise ValueError("Artifact manifest does not match the package catalog.")
    for entry in manifest["packages"]:
        if entry["file"] != f"{entry['id']}.{value}.nupkg":
            raise ValueError("Unexpected artifact filename.")
        if hashlib.sha256((PACKAGES / entry["file"]).read_bytes()).hexdigest() != entry["sha256"]:
            raise ValueError(f"Artifact hash mismatch: {entry['file']}")
    return manifest


def remote_package(entry, value):
    name = entry["id"].lower()
    url = f"https://api.nuget.org/v3-flatcontainer/{name}/{value}/{name}.{value}.nupkg"
    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def same_contents(local, remote):
    # NuGet adds a repository signature; all unsigned package contents must still match.
    def contents(data):
        with ZipFile(io.BytesIO(data)) as archive:
            return {name: archive.read(name) for name in archive.namelist() if name != ".signature.p7s"}
    if contents(local) != contents(remote):
        raise ValueError("NuGet already has this version with different contents. Use a new version.")


def wait_for_package(entry, value):
    deadline = time.monotonic() + 600
    while time.monotonic() < deadline:
        remote = remote_package(entry, value)
        if remote is not None:
            same_contents((PACKAGES / entry["file"]).read_bytes(), remote)
            return
        time.sleep(10)
    raise TimeoutError(f"NuGet has not made {entry['id']} {value} available after 10 minutes. Rerun failed jobs.")


def publish(value, group):
    manifest = artifacts(value)
    key = os.environ.get("NUGET_API_KEY")
    if not key:
        raise ValueError("Missing short-lived NuGet credential. Authenticate with NuGet/login before publishing.")
    for entry in manifest["packages"]:
        if entry["group"] != group:
            continue
        local = (PACKAGES / entry["file"]).read_bytes()
        remote = remote_package(entry, value)
        if remote is not None:
            same_contents(local, remote)
            print(f"Already published and verified: {entry['id']} {value}")
            continue
        print(f"Publishing {entry['id']} {value}", flush=True)
        # Never include the API key in a Python exception or diagnostic command line.
        result = subprocess.run(["dotnet", "nuget", "push", str(PACKAGES / entry["file"]),
                                 "--source", NUGET, "--api-key", key], cwd=ROOT)
        if result.returncode:
            raise RuntimeError(f"NuGet push failed for {entry['id']}; rerun failed jobs to resume.")
        wait_for_package(entry, value)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["event", "pack", "inspect", "smoke", "test", "publish", "verify"])
    parser.add_argument("--version", type=version)
    parser.add_argument("--group", choices=["core", "mise", "partie", "expo", "extensions"])
    args = parser.parse_args()
    if args.command == "event":
        release_event()
        return
    if not args.version:
        parser.error("--version is required")
    if args.command == "publish":
        if not args.group:
            parser.error("--group is required for publish")
        publish(args.version, args.group)
    elif args.command == "verify":
        manifest = artifacts(args.version)
        for entry in manifest["packages"]:
            wait_for_package(entry, args.version)
        message = f"Verified {len(manifest['packages'])} NuGet packages at {args.version}, from commit {manifest['commit']}."
        print(message)
        if os.environ.get("GITHUB_STEP_SUMMARY"):
            with Path(os.environ["GITHUB_STEP_SUMMARY"]).open("a") as output:
                output.write(message + "\n\nDownload the release-packages artifact for package files and hashes.\n")
    else:
        {"pack": pack, "inspect": inspect, "smoke": smoke, "test": tests}[args.command](args.version)


if __name__ == "__main__":
    main()
