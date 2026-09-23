# 03 Generator core

Depends: 02.

- [ ] Build shared incremental Roslyn helpers with `ForAttributeWithMetadataName`. Parse core table, column, alias, relationship, and row attributes plus engine-owned attributes into immutable models.
- [ ] Expose helper APIs to engine generators; do not register a standalone core generator. Engine generators own `IIncrementalGenerator.Initialize` and source output registration.
- [ ] Give engines hooks to parse their runtime attributes, validate dialect metadata, contribute diagnostics, and emit extra members without copying core symbol traversal.
- [ ] Centralize fully qualified symbol matching, nullable annotations, accessibility, constructor/member selection, identifier comparison, stable declaration ordering, hint names, and C# literal/identifier escaping.
- [ ] Assign stable `MISE` diagnostic IDs. Each descriptor has a fixed category, severity, enabled state, message, and source location policy. Cover partial/container failures, invalid metadata combinations, duplicate names/aliases, unresolved relationship members, invalid keys, unsupported row shapes, and ambiguous construction.
- [ ] Do not use `CompilationProvider` or scan every syntax tree. Combine only the semantic inputs and engine options needed for one target. Add stable equality to models so unchanged targets stay cached.
- [ ] Emit one deterministic hint per mapped target. Two targets with the same short name in different namespaces or containing types must receive distinct hint names.
- [ ] Check cancellation during symbol traversal and large emission loops.
- [ ] Provide shared analyzer support for `FormattableString` calls. Report an error when a `:raw` hole is not a compile-time constant string. Accept const strings emitted by Mise generators.

Tests:

- [ ] Generator-driver tests assert each diagnostic ID, severity, message arguments, and source span, plus absence of generated output for fatal diagnostics.
- [ ] Generated-output tests cover classes, structs, record classes/structs, nested and generic types, inheritance, nullable/required members, aliases, relationships, composite keys, escaped C# identifiers, and hostile database identifiers.
- [ ] Raw-interpolation analyzer tests cover string literals, user const fields, generated const fields, variables, properties, method returns, non-string constants, and diagnostic locations.
- [ ] Every generated case compiles with nullable warnings enabled and no compilation errors.
- [ ] Determinism tests run shuffled syntax-tree inputs and repeated driver runs, then compare hint names and source byte-for-byte.
- [ ] Incremental tracking tests change one target and prove unrelated targets are cached rather than regenerated.

Done: all five engine generators can pass the same source through the shared pipeline, attach engine metadata, emit deterministic compiling output, and reuse unchanged incremental steps without copying parser code.
