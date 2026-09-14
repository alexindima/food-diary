# BodyMetrics Application Guidelines

## Scope

Rules for `Modules/BodyMetrics/Application/`.

## Boundaries

- Own weight and waist commands, queries, handlers, validation, mappings, and read services.
- Consume shared user access only through application abstractions.
- Preserve calculation, date normalization, authorization, and user-scoping semantics.
- Use `FoodDiary.Modules.BodyMetrics.Application` with project-relative namespaces; keep ports in the sibling Application.Abstractions project.
- Keep persistence implementations and HTTP transport outside this project.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.

Public ReadWeight*/ReadWaist* request handlers own projection and aggregation logic. Existing user-facing handlers retain their access checks and date normalization and dispatch these composition queries; foreign modules do not consume projection repository ports.
