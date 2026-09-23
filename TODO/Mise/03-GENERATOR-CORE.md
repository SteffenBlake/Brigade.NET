# 03 Generator core

Depends: 02.

- [x] Build shared incremental Roslyn helpers with `ForAttributeWithMetadataName`. Each engine passes its table and row attribute metadata names into the shared pipeline. Parse engine table and row metadata plus core column, alias, and relationship attributes into immutable models.
- [x] Expose helper APIs to engine generators; do not register a standalone core generator. Engine generators own `IIncrementalGenerator.Initialize` and source output registration.
- [x] Give engines hooks to parse their runtime attributes, validate dialect metadata, contribute diagnostics, and emit extra members without copying core symbol traversal.
- [x] Centralize fully qualified symbol matching, nullable annotations, accessibility, constructor/member selection, identifier comparison, stable declaration ordering, hint names, and C# literal/identifier escaping.
- [x] Assign stable `MISE` diagnostic IDs. Each descriptor has a fixed category, severity, enabled state, message, and source location policy. Cover partial/container failures, invalid metadata combinations, duplicate names/aliases, unresolved relationship members, invalid keys, unsupported row shapes, and ambiguous construction.
- [x] Do not use `CompilationProvider` or scan every syntax tree. Combine only the semantic inputs and engine options needed for one target. Add stable equality to models so unchanged targets stay cached.
- [x] Emit one readable, consistently formatted file per mapped source target. Use four-space indentation and LF line endings. Do not combine unrelated targets into a monolithic generated file. Two targets with the same short name in different namespaces or containing types must receive distinct deterministic hint names.
- [x] Check cancellation during symbol traversal and large emission loops.
- [x] Provide shared analyzer support for `FormattableString` calls. Report an error when a `:raw` hole is not a compile-time constant string. Accept const strings emitted by Mise generators.

Tests:

- [x] Generator-driver tests assert each diagnostic ID, severity, message arguments, and source span, plus absence of generated output for fatal diagnostics.
- [x] Generated-output tests cover classes, structs, record classes/structs, nested and generic types, inheritance, nullable/required members, aliases, relationships, composite keys, escaped C# identifiers, and hostile database identifiers.
- [x] Raw-interpolation analyzer tests cover string literals, user const fields, generated const fields, variables, properties, method returns, non-string constants, and diagnostic locations.
- [x] Every generated case compiles with nullable warnings enabled and no compilation errors.
- [x] Determinism tests run shuffled syntax-tree inputs and repeated driver runs, then compare hint names and source byte-for-byte.
- [x] Formatting tests inspect generated files for human-readable layout, four-space indentation, LF line endings, and one-to-one target/file parity.
- [x] Incremental tracking tests change one target and prove unrelated targets are cached rather than regenerated.
- [x] Multi-engine tests run the shared behavior suite for all five engines, prove each generator ignores other engines' table and row attributes, and reject conflicting engine markers on one type.

Done: all five engine generators can pass the same source through the shared pipeline, attach engine metadata, emit deterministic compiling output, and reuse unchanged incremental steps without copying parser code.
