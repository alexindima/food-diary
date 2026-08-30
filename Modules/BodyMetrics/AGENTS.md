# BodyMetrics Logical Module Guidelines

## Scope

Rules for `Modules/BodyMetrics/`.

## Boundaries

- Own weight and waist entry use cases, application contracts, persistence adapters, and EF entry configurations.
- Preserve `WeightEntries` and `WaistEntries` as the two cohesive feature groups.
- Preserve the legacy `FoodDiary.Application.BodyMetrics` assembly name and CLR namespaces.
- Keep `WeightEntry`, `WaistEntry`, their IDs, goal lifecycle types, and the public `User` navigations in central Domain until that aggregate boundary can change without CLR or EF compatibility breaks.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Register application and persistence through Infrastructure's `AddBodyMetricsModule` facade.
- Treat body measurements as private health data: preserve current-user authorization and user-scoped repository predicates.

## Tests

- Keep BodyMetrics-owned application tests under this module's `tests/` folder.
- Keep central Domain invariant tests central while their production types remain central.
- Keep shared DbContext, migration, HTTP, host, architecture, and cross-module scenarios in central test projects.
