# 01 Projects

Depends: none.

- [x] Add .NET 10 runtime projects `Brigade.Net.Mise`, `Brigade.Net.Mise.SqlServer`, `.PostgreSQL`, `.SQLite`, `.MySQL`, and `.MariaDb`. Runtime engine projects reference core Mise and exactly one ADO.NET provider package.
- [x] Add `Brigade.Net.Mise.Generator`, targeting `netstandard2.0`, for shared Roslyn models, diagnostics, parsing, and emission helpers. It is a build-time implementation assembly, not a consumer runtime dependency.
- [x] Add `netstandard2.0` Roslyn projects `Brigade.Net.Mise.Engines.SqlServer`, `.PostgreSQL`, `.SQLite`, `.MySQL`, and `.MariaDb`, with `IsRoslynComponent=true` and `IncludeBuildOutput=false`.
- [x] Each engine analyzer references `Brigade.Net.Mise.Generator` and includes that DLL in analyzer output using the same dependency-bundling pattern as `Brigade.Net.Partie.Engines.AspNetCore`.
- [x] Add .NET 10 `Brigade.Net.Partie.Extensions.Mise`, referencing Partie and Mise. It must not depend on an engine package.
- [x] Add xUnit projects for core runtime, shared generator behavior, each engine generator, Partie integration, and database integration. Mark test projects non-packable and use the repository package versions and global `Xunit` using pattern.
- [x] Reference analyzer projects in generator-consumer tests with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`. Direct generator references are allowed only in generator-driver test projects.
- [x] Add every source and test project to `Brigade.NET.slnx`. Keep project settings local because this repository has no central build props file.
- [x] Enforce dependency direction: runtime engine -> Mise runtime; engine analyzer -> generator helper; Partie extension -> Partie + Mise. No runtime assembly references any Roslyn assembly.
- [x] Add XML documentation generation and `CS1591` as an error to projects that expose public runtime API, matching existing Partie runtime projects.

Tests:

- [x] A dependency test inspects project/package references and fails on a runtime-to-Roslyn reference or an engine dependency in Mise core.
- [x] A clean fixture references each packed runtime/analyzer pair, restores without project references, compiles a mapped type, and observes generated members.
- [x] The fixture does not reference `Brigade.Net.Mise.Generator` directly.

Done: clean restore/build passes, all projects appear in the solution, dependency tests pass, and every packed analyzer loads with its helper dependencies in the clean fixture.
