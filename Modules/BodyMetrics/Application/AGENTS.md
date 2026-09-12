# BodyMetrics Application Guidelines

## Scope

Rules for `Modules/BodyMetrics/Application/`.

## Boundaries

- Own weight and waist commands, queries, handlers, validation, mappings, and read services.
- Consume shared user access only through application abstractions.
- Preserve calculation, date normalization, authorization, and user-scoping semantics.
- Preserve the legacy `FoodDiary.Application.BodyMetrics` assembly name and CLR namespaces.
- Keep persistence implementations and HTTP transport outside this project.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.
