---
name: writing-unit-tests
description: "Use when writing unit tests / checking coverage for this repo. Covers coverage target + script to find uncovered lines."
---

# Writing Unit Tests 🧪

## Target 🎯

- Line coverage > 95%
- Branch coverage > 95%
- Good 'nuf. Don't chase 100% w/ fake/meaningless test (e.g. force null on non-nullable invariant).

## Find missed spot 🔎

Run script, give it test csproj path:

```
python3 .github/skills/writing-unit-tests/scripts/find_uncovered.py path/to/Foo.Tests.csproj
```

Script do:
1. `dotnet test` w/ coverage collector
2. Parse cobertura xml
3. Print overall line-rate + branch-rate
4. Print EVERY file w/ missed line# + partial branch line#

Use output ➡️ go straight to missed line# ➡️ write test hit it.

## Rule

Don't test private/internal impl detail directly. Cover via public API call only.
