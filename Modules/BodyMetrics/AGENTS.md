# BodyMetrics Logical Module Guidelines

## Scope

Rules for `Modules/BodyMetrics/`.

## Boundaries

- Own weight and waist entry use cases, application contracts, persistence adapters, and EF entry configurations.
- Preserve `WeightEntries` and `WaistEntries` as the two cohesive feature groups.
- External read services and immutable entry/summary results live in BodyMetrics.Contracts, which cannot reference aggregate-bearing Domain or repository ports.
- WeightEntryErrors and WaistEntryErrors stay in the corresponding owner Abstractions groups. Call them directly; central Errors.WeightEntry/Errors.WaistEntry facades and the central BodyMetrics ports reference are retired. Preserve error codes/messages/kinds and invariant date formatting; see docs/ai/measurement-error-facades.md.
- Use `FoodDiary.Modules.BodyMetrics.<Project>` assembly names and namespaces matching project-relative folders, including tests. Do not override RootNamespace or AssemblyName to retain donor identities.
- Keep all projects in sibling directories, including Application.Abstractions and PersistenceModel; do not restore nested projects or Compile Remove exclusions.
- Keep `WeightEntry`, `WaistEntry`, and their IDs in module-owned `Domain` under `FoodDiary.Modules.BodyMetrics.Domain` with project-relative namespaces. The module depends on Users Domain.Contracts for scalar `UserId`; do not restore the removed inverse measurement navigations.
- Keep `WeightGoal`, `WaistGoal`, their IDs and lifecycle/status behavior, and the public goal navigations in Users Domain as User-owned responsibilities.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Runtime measurement repositories use BodyMetricsDbContext, sharing the scoped connection and atomic IUnitOfWork with Hydration and central Infrastructure (ADR 0040). Central mappings remain for migration and composed reads; owner purge uses the live shared transaction and goals remain Users-owned.
- Register application and persistence through Infrastructure's `AddBodyMetricsModule` facade.
- Expose separate write and read-model repository ports backed by one scoped implementation; do not restore unused combined/read-entity ports. Preserve the shared weight/waist read services consumed by local handlers and other modules.
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
