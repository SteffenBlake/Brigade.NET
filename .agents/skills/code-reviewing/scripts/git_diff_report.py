#!/usr/bin/env python3
"""Compact git diff report for .cs/.csproj/.sln/.slnx/.json files.

Prints unified diffs w/ extra context lines, easy for agent to parse.
Usage:
    python3 git_diff_report.py [<git-diff-args>...]

Examples:
    python3 git_diff_report.py               # working tree vs HEAD
    python3 git_diff_report.py --staged      # staged vs HEAD
    python3 git_diff_report.py main..HEAD    # compare branches
"""
import subprocess
import sys

EXTS = (".cs", ".csproj", ".sln", ".slnx", ".json")
CONTEXT = 8  # lines of context before/after each hunk


def run_git(args):
    result = subprocess.run(
        ["git"] + args, capture_output=True, text=True, check=False
    )
    if result.returncode != 0 and result.stderr.strip():
        print(result.stderr, file=sys.stderr)
    return result.stdout


def get_changed_files(diff_args):
    out = run_git(["diff", "--name-only"] + diff_args)
    return [f for f in out.splitlines() if f.strip().endswith(EXTS)]


def get_untracked_files():
    """New files git status marks '??' — no diff_args since they aren't tracked yet."""
    out = run_git(["status", "--porcelain", "--untracked-files=all"])
    files = []
    for line in out.splitlines():
        if line.startswith("??"):
            path = line[3:].strip()
            if path.endswith(EXTS):
                files.append(path)
    return files


def print_file_diff(path, diff_args):
    diff = run_git(
        ["diff", f"--unified={CONTEXT}", "--no-color"] + diff_args + ["--", path]
    )
    if not diff.strip():
        return
    print(f"### FILE: {path}")
    for line in diff.splitlines():
        # Skip noisy header lines, keep hunk markers + content
        if line.startswith(("diff --git", "index ", "--- ", "+++ ")):
            continue
        print(line)
    print()


def print_new_file(path):
    """Untracked file: show as a full add, no --- /dev/null noise."""
    diff = run_git(["diff", "--no-color", "--no-index", "/dev/null", path])
    if not diff.strip():
        return
    print(f"### FILE: {path} (new)")
    for line in diff.splitlines():
        if line.startswith(("diff --git", "index ", "--- ", "+++ ")):
            continue
        print(line)
    print()


def main():
    diff_args = sys.argv[1:]
    files = get_changed_files(diff_args)
    untracked = get_untracked_files()
    if not files and not untracked:
        print("No changed .cs/.csproj/.sln/.slnx/.json files.")
        return
    for f in files:
        print_file_diff(f, diff_args)
    for f in untracked:
        print_new_file(f)


if __name__ == "__main__":
    main()
