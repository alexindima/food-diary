# Body Metrics Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.BodyMetrics/`.

## Role

- Own weight and waist tracking use cases in one cohesive physical module.
- Preserve `WeightEntries` and `WaistEntries` as separate logical feature areas.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddBodyMetricsModule`.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.
