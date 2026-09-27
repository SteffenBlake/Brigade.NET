#!/usr/bin/env python3
"""Run once: python3 Benchmarks/Api/calibrate.py. Requires dotnet, Docker and k6."""

import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import time
from datetime import datetime, timezone

BASELINE = 1000
STEP = 200
SECONDS = 5
ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "Benchmarks/Api/Runner/Brigade.Net.Benchmarks.Api.Runner.csproj"


def worker():
    """Act as the runner's k6 executable, keeping Aspire alive across rate steps."""
    executable = os.environ["CALIBRATE_K6"]
    if sys.argv[2:] == ["version"]:
        return subprocess.call([executable, "version"])
    environment = os.environ.copy()
    directory = Path(environment["CALIBRATE_DIRECTORY"])
    if environment.get("WARMUP") == "1":
        environment.update(RATE=str(BASELINE), DURATION="1s")
        return subprocess.call([executable, *sys.argv[2:]], env=environment)

    state_file = directory / "calibration.json"
    state = json.loads(state_file.read_text())
    label = Path(environment["SUMMARY_FILE"]).stem
    def progress(message):
        with (directory / "progress.log").open("a") as output:
            output.write(message + "\n")
    rate = BASELINE
    passed = None
    while state["ceiling"] is None or rate <= state["ceiling"]:
        progress(f"[{len(state['cases']) + 1}/30] {label}: {rate:,} RPS for {SECONDS}s")
        stream = directory / f"{label}-{rate}-metrics.jsonl"
        summary = directory / f"{label}-{rate}-summary.json"
        environment.update(RATE=str(rate), DURATION=f"{SECONDS}s", SUMMARY_FILE=str(summary))
        process = subprocess.Popen(
            [executable, "run", "--quiet", "--out", f"json={stream}",
             "--summary-trend-stats", "avg,min,med,max,p(90),p(95),p(99)",
             str(directory / "step.js")], env=environment,
        )
        missed = False
        reader = None
        try:
            while process.poll() is None:
                if reader is None and stream.exists():
                    reader = stream.open()
                if reader is not None:
                    while True:
                        position = reader.tell()
                        line = reader.readline()
                        if not line or not line.endswith("\n"):
                            reader.seek(position)
                            break
                        point = json.loads(line)
                        if point.get("type") != "Point":
                            continue
                        value = point["data"]["value"]
                        metric = point["metric"]
                        if ((metric in ("dropped_iterations", "http_req_failed") and value > 0)
                                or (metric == "checks" and value == 0)):
                            missed = True
                            process.send_signal(signal.SIGINT)
                            break
                if missed:
                    try:
                        process.wait(timeout=3)
                    except subprocess.TimeoutExpired:
                        process.kill()
                    break
                time.sleep(0.05)
            process.wait()
        finally:
            if reader:
                reader.close()
            if process.poll() is None:
                process.kill()
                process.wait()

        # Read the final summary too, covering points flushed at process exit.
        if summary.exists():
            metrics = json.loads(summary.read_text())["metrics"]
            def values(name):
                item = metrics.get(name, {})
                return item.get("values", item)
            missed |= (values("dropped_iterations").get("count", 0) > 0
                       or values("http_req_failed").get("rate", 0) > 0
                       or values("checks").get("fails", 0) > 0)
        if missed:
            state["ceiling"] = rate - STEP
            progress(f"First missed request at {rate:,} RPS; shared ceiling {rate - STEP:,}")
            break
        if process.returncode != 0 or not summary.exists():
            raise RuntimeError(f"k6 failed during calibration of {label} at {rate} RPS")
        if values("http_reqs").get("count", 0) < rate * SECONDS * 0.99:
            raise RuntimeError(f"Incomplete request schedule for {label} at {rate} RPS")
        passed = summary
        rate += STEP

    state["cases"].append({"case": label, "lastPassingRps": rate - STEP if passed else None,
                           "firstMissedRps": rate if missed else None})
    state_file.write_text(json.dumps(state, indent=2) + "\n")
    if passed is None:
        raise RuntimeError(f"{label} failed the {BASELINE} RPS baseline; choose a lower baseline")
    # The existing runner receives the last successful step; calibration owns its separate data.
    shutil.copyfile(passed, os.environ["SUMMARY_FILE"])
    return 0


def main():
    executable = shutil.which(os.environ.get("BENCH_K6", "k6"))
    if not executable:
        raise RuntimeError("Install k6 or set BENCH_K6 to its executable path")
    directory = ROOT / "Benchmarks/.work/calibration" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    directory.mkdir(parents=True)
    state_file = directory / "calibration.json"
    state_file.write_text(json.dumps({"ceiling": None, "cases": []}))
    load = ROOT / "Benchmarks/Api/Load/load.js"
    (directory / "step.js").write_text(
        f"import run, {{options as original, handleSummary}} from {json.dumps(load.as_uri())};\n"
        "export {handleSummary};\n"
        "export const options = {...original, thresholds: {}, scenarios: {requests: "
        "{...original.scenarios.requests, gracefulStop: '5s'}}};\n"
        "export default run;\n"
    )
    wrapper = directory / "k6-wrapper"
    import shlex
    wrapper.write_text(f"#!/bin/sh\nexec {shlex.quote(sys.executable)} {shlex.quote(str(Path(__file__).resolve()))} --worker \"$@\"\n")
    wrapper.chmod(0o755)
    environment = os.environ.copy()
    environment.update(BENCH_K6=str(wrapper), CALIBRATE_K6=executable,
                       CALIBRATE_DIRECTORY=str(directory), BENCH_RESULTS=str(directory / "runner"),
                       BENCH_RPS=str(BASELINE), BENCH_REPEATS="1",
                       BENCH_DURATION_SECONDS=str(SECONDS), BENCH_WARMUP_SECONDS="1")
    progress_file = directory / "progress.log"
    progress_file.touch()
    process = subprocess.Popen(["dotnet", "run", "--project", str(PROJECT), "-c", "Release"],
                               cwd=ROOT, env=environment)
    try:
        with progress_file.open() as reader:
            while process.poll() is None:
                for line in reader:
                    print(line.rstrip(), flush=True)
                time.sleep(0.1)
            for line in reader:
                print(line.rstrip(), flush=True)
        if process.returncode != 0:
            raise RuntimeError(f"Aspire runner exited with code {process.returncode}; inspect {directory}")
    finally:
        if process.poll() is None:
            process.send_signal(signal.SIGINT)
            process.wait()
    state = json.loads(state_file.read_text())
    if len(state["cases"]) != 30 or state["ceiling"] is None:
        raise RuntimeError("Calibration did not establish a shared ceiling across all 30 cases")
    suggested = state["ceiling"] - STEP
    if suggested <= 0:
        raise RuntimeError("Calibration requires a lower baseline")
    state["suggestedRps"] = suggested
    state_file.write_text(json.dumps(state, indent=2) + "\n")
    print(f"\nSuggested benchmark configuration: BENCH_RPS={suggested}")
    print(f"Calibration saved to {state_file}")


if __name__ == "__main__":
    try:
        sys.exit(worker() if sys.argv[1:2] == ["--worker"] else main())
    except (RuntimeError, subprocess.CalledProcessError) as error:
        print(f"Calibration failed: {error}", file=sys.stderr)
        sys.exit(1)
