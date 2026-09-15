# Reusable presentation mappings

Own pure application-model to HTTP-response transformations. Depend only on narrow
application/scalar contracts, HTTP DTO contracts and other pure Presentation.Mappings.
Never reference a whole Application, Domain, Presentation, Infrastructure or host
assembly; no ASP.NET framework, DI, I/O or side effects. Request-to-command mapping
stays in Presentation. Preserve nulls, ordering, dates, enum encodings and wire fields.

Current module convention: all projects use `FoodDiary.Modules.Tdee.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
