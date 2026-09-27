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
- Aim higher when useful, but never trade honest coverage for a higher percentage.

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

Use each missed location to find a real behavior or error path. Test the observable result, not the location itself.

For layer totals across unit suites, merge their fresh JSON reports from the same code build:
`python3 .agents/skills/writing-unit-tests/scripts/find_uncovered.py --merge path/to/coverage.json ...`
Never mix stale reports or include AppHost integration coverage.

## Rule

Test private/internal code through public behavior. Never add `InternalsVisibleTo`, make a member public, use reflection or access-bypass attributes, or add a test-only hook to reach it for coverage. If no public path uses it, report that gap and assess whether the code is needed; do not invent a path just to hit it.

Do not inflate coverage with tests that merely construct a type, call a member, or assert its implementation instead of a meaningful outcome. Do not force impossible states, fake provider/compiler inputs unrelated to a supported error path, or weaken production behavior to make a branch reachable. Do not remove useful guards or code solely to erase missed points.

Do not hide gaps by excluding source, suppressing instrumentation or sequence points, altering filters or thresholds, merging stale reports, or skipping/failing to run relevant tests. For compiler-generated or genuinely unreachable points, state the measured limit and why it exists. Keep the raw report visible; never claim 100% without a fresh, complete run that measures it.

Application fixture data follows the example's CQRS names, versioned folders, and per-operation DTO ownership. Child DTOs stay with their owner. Validation examples use a local `IValidatable.Validate()` returning `Result<Unit>` and a generic `ValidationPartie`. Clearly mark deliberately invalid compiler inputs.
