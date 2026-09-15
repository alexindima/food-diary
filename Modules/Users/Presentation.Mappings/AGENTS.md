# Reusable presentation mappings

Own pure application-model to HTTP-response transformations. Depend only on narrow
application/scalar contracts, HTTP DTO contracts and other pure Presentation.Mappings.
Never reference a whole Application, Domain, Presentation, Infrastructure or host
assembly; no ASP.NET framework, DI, I/O or side effects. Request-to-command mapping
stays in Presentation. Preserve nulls, ordering, dates, enum encodings and wire fields.

All module projects and tests use `FoodDiary.Modules.Users.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
