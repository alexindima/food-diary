# TDEE Logical Module Guidelines

## Scope

Rules for `Modules/Tdee/`.

## Boundaries

- Own TDEE calculation, insight models, and their use cases.
- Keep the real application assembly at `Application/FoodDiary.Modules.Tdee.Application.csproj`; do not recreate a root module project or empty wrapper layers.
- Depend on Exercises through `Modules/Exercises/Contracts`, on daily calories through `Modules/Meals/Contracts`, and on user/weight data through their existing application contracts.
- Do not reference the core `FoodDiary.Application` project.
- Preserve the legacy `FoodDiary.Application.Tdee` assembly name and `FoodDiary.Application.Tdee.*` CLR namespaces during this extraction.
- Register handlers, validators, and profile services through `AddTdeeModule`.
- Contracts owns the proven Dashboard read API. Do not add other layers without an owned port, domain type or adapter.

## Tests

- Keep TDEE-owned application tests under `tests/FoodDiary.Modules.Tdee.Application.Tests` in this module.
- Keep Dashboard composition, HTTP, host, architecture, and cross-module tests in their central test projects.

## Consumer boundary

Own TdeeInsightModel, TdeeConfidence and GetTdeeInsightQuery consumed by Dashboard. Keep calculations, repositories and handlers in Application. See `Contracts/AGENTS.md` and ADR 0033.
