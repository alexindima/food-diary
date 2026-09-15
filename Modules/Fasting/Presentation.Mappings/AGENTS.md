# Reusable presentation mappings

Own pure application-model to HTTP-response transformations. Depend only on narrow
application/scalar contracts, HTTP DTO contracts and other pure Presentation.Mappings.
Never reference a whole Application, Domain, Presentation, Infrastructure or host
assembly; no ASP.NET framework, DI, I/O or side effects. Request-to-command mapping
stays in Presentation. Preserve nulls, ordering, dates, enum encodings and wire fields.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
