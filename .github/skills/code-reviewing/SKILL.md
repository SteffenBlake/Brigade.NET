---
name: code-reviewing
description: "Use when doing code review on this repo. Covers diff script, pass tests, coverage check."
---

## Dependant skills 🔗

Always ensure these skills loaded too: `caveman-speak`, `csharp-standards`, `writing-unit-tests`.

## Step 1: See what change 👀

Run script, get diff for .cs, .csproj, .sln, .slnx, .json file. Script gives line#, extra context around change, minimal token, easy parse.

```
python3 .github/skills/code-reviewing/scripts/git_diff_report.py
```

Add git-diff arg if need (e.g. `--staged`, `main..HEAD`).

Read output top to bottom. Each file block start `### FILE: path`. Hunk line `@@ -old,+new @@` show line#. `+` line new. `-` line old. Blank line context.

Count file block total first (`grep -c '^### FILE' <output>`). Review EVERY block, one by one. No skim. No skip big file. No stop early cuz token cost. 100% file, 100% hunk, every time. Big diff no excuse.

CRITICAL: FILE > 2000 lines, STOP, REPORT TO USER, CONFIRM OKAY

TRICK: READ FILE ALL AT ONCE, ONE TOOL CALL, USE LINECOUNT 10,000, ONE TOOL CALL = MUCH LESS TOKENS BURNT

## Step 2: Tests must be green ✅

Run full test suite. ALL test pass. No exception. No skip.

```
dotnet test
```

Fail test found? REPORT ONLY. Never touch code or test yourself. Editing code forbidden, always, no exception.

## Step 3: Coverage check 📊

Need line coverage > 95% AND branch coverage > 95%.

Load skill `writing-unit-tests` for python script that find uncovered line/branch (`find_uncovered.py`). Use it, point at test csproj, REPORT gap found. Never write test yourself.

## Step 4: Write report 📝

Write finding to `/tmp/CODE-REVIEW.MD`. Timestamp at very top. Whole report in caveman speak (short word, no fluff, like this skill file talk).

## Rule 🪨

- Reviewer role only. Never edit code, never edit test, never fix anything. Report finding, that's it.
- Review diff BEFORE run test/coverage — know what change first.
- Small diff, small review. Big diff, go slow, check each hunk.
- ALL FILE MUST REVIEW. No lazy, no skip, no "representative sample". Miss one file = fail review job.
- Before write report, double check file count reviewed == file count in diff output.
