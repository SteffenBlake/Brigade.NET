# Production reachability check

Run from the repository root:

```sh
dotnet test Tests/Architecture.Tests/Brigade.Net.Architecture.Tests.csproj
```

The solution and existing PR test discovery include this project. It loads every
production project under `Source` through `MSBuildWorkspace`, runs source
generators through Roslyn compilation, and rejects workspace or compilation
errors before reporting unreachable types. It needs the repository checkout and
the SDK selected by `global.json`.

Roots are public types in runtime projects, types marked with Roslyn's
`GeneratorAttribute` or `DiagnosticAnalyzerAttribute`, and the compiler's
`IsExternalInit` shim. Public types in
`.Generator` and `.Engines.` projects do not become roots just because they are
public. The graph follows semantic type and member references, generic arguments,
attributes, and nested types across production assemblies. Tests never provide
reachability roots. Isolated groups of types are reported together even when they
reference each other.

A finding means a type has no path under these rules and needs review. The test
does not delete code or silently baseline findings. Failures include symbol and
file location, so CI displays the same evidence as a local run.

This is a conservative type-level check: it does not find unused members within
a reachable type or unused public runtime APIs. References in unused methods can
keep a type reachable. Reflection, names embedded in generated source strings,
and external consumers cannot be inferred from ordinary semantic references;
review those cases before removing a flagged type. Generated syntax trees are
included as graph edges, but generated files are not deletion candidates. The
check compiles the Source projects rather than every possible
consumer application. Intentional extra roots need an explicit rule and a
regression test, rather than a blanket suppression.
