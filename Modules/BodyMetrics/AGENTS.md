# BodyMetrics Logical Module Guidelines

## Scope

Rules for `Modules/BodyMetrics/`.

## Boundaries

- Own weight and waist entry use cases, application contracts, persistence adapters, and EF entry configurations.
- Preserve `WeightEntries` and `WaistEntries` as the two cohesive feature groups.
- Preserve the legacy `FoodDiary.Application.BodyMetrics` assembly name and CLR namespaces.
- Keep `WeightEntry`, `WaistEntry`, and their IDs in module-owned `Domain` with their legacy CLR namespaces. The module depends one-way on Users-owned `User`/`UserId`; do not restore the removed inverse measurement navigations.
- Keep `WeightGoal`, `WaistGoal`, their IDs and lifecycle/status behavior, and the public goal navigations in Users Domain as User-owned responsibilities.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Register application and persistence through Infrastructure's `AddBodyMetricsModule` facade.
- Treat body measurements as private health data: preserve current-user authorization and user-scoped repository predicates.

## Tests

- Keep BodyMetrics-owned application and focused measurement-domain tests under this module's `tests/` folder.
- Keep shared DbContext, migration, HTTP, host, architecture, and cross-module scenarios in central test projects.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
