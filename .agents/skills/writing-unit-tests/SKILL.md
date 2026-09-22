---
name: writing-unit-tests
description: "Use when writing unit tests / checking coverage for this repo. Covers coverage target + script to find uncovered lines."
metadata:
  internal: true
---

# Writing Unit Tests 🧪

## Target 🎯

- Line coverage > 95%
- Branch coverage > 95%
- AppHost integration tests: ignore coverage. They launch other processes, so host coverage does not measure the app. Require every integration test to pass; no skipped tests.
- Good 'nuf. Don't chase 100% w/ fake/meaningless test (e.g. force null on non-nullable invariant).

## Find missed spot 🔎

Run script, give it test csproj path:

```
python3 .agents/skills/writing-unit-tests/scripts/find_uncovered.py path/to/Foo.Tests.csproj
```

Script do:
1. `dotnet test` w/ coverage collector
2. Parse cobertura xml
3. Print overall line-rate + branch-rate
4. Print EVERY file w/ missed line# + partial branch line#

Use output ➡️ go straight to missed line# ➡️ write test hit it.

For layer totals across unit suites, merge their fresh JSON reports from the same code build:
`python3 .agents/skills/writing-unit-tests/scripts/find_uncovered.py --merge path/to/coverage.json ...`
Never mix stale reports or include AppHost integration coverage.

## Rule

Don't test private/internal impl detail directly. Cover via public API call only.

Application fixture data follows the example's CQRS names, versioned folders, and per-operation DTO ownership. Child DTOs stay with their owner. Validation examples use a local `IValidatable.Validate()` returning `Result<Unit>` and a generic `ValidationPartie`. Clearly mark deliberately invalid compiler inputs.
