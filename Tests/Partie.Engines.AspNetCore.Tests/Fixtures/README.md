# Application test fixtures

Use the same CQRS layout as the example: domain slice, operation, version, then
`Cmd`, `Query`, `Result`, or `Handler`. Each operation owns its request DTO and child
types. Create returns an ID; Update and Delete return `Unit`. Shared providers and
services live above the operation folders.

`IValidatable` and `ValidationPartie` are local test-domain fixtures. Both query and
command hooks run `Validate()` when the request implements the interface, then
use `FlatMapAsync` to continue on success. Requests without validation pass through.

The Items Search route has a category path parameter and a mode query parameter
to exercise independent ASP.NET binding sources. Mutation fixtures carry a body
on each verb to exercise binding for POST, PUT, PATCH, and DELETE. `Counts.Observed`
records bound values for assertions without putting test observations in response DTOs.

Compiler tests elsewhere embed source text, including intentionally invalid inputs
and low-level graph API contracts. Those strings exercise the compiler diagnostics
or graph API described by their test names.
