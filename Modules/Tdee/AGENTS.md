# TDEE Logical Module Guidelines

## Scope

Rules for `Modules/Tdee/`.

## Boundaries

- Own TDEE calculation, insight models, and their use cases.
- Keep the real application assembly at `Application/FoodDiary.Modules.Tdee.Application.csproj`; do not recreate a root module project or empty wrapper layers.
- Depend on Exercises through `FoodDiary.Application.Exercises` and on user, weight, and dashboard data through `FoodDiary.Application.Abstractions` contracts.
- Do not reference the core `FoodDiary.Application` project.
- Preserve the legacy `FoodDiary.Application.Tdee` assembly name and `FoodDiary.Application.Tdee.*` CLR namespaces during this extraction.
- Register handlers, validators, and profile services through `AddTdeeModule`.
- Do not add Contracts, Application Abstractions, Domain, or Infrastructure projects unless TDEE gains a separately proven stable consumer contract, owned port, domain type, or adapter.

## Tests

- Keep TDEE-owned application tests under `tests/FoodDiary.Modules.Tdee.Application.Tests` in this module.
- Keep Dashboard composition, HTTP, host, architecture, and cross-module tests in their central test projects.
