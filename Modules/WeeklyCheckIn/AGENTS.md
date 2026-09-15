# Weekly Check-In Logical Module Guidelines

## Scope

Rules for `Modules/WeeklyCheckIn/`.

## Boundary

- Own weekly check-in queries, models, calculations, and user-profile composition in `Application/`.
- Keep the real application assembly at `Application/FoodDiary.Modules.WeeklyCheckIn.Application.csproj`; do not recreate a root module project or empty wrapper layers.
- Read hydration through `Modules/Hydration/Contracts` and meal, dashboard statistics, body-metric, and user-profile data only through stable Contracts/Application Abstractions.
- Do not load Hydration or Meal aggregates.
- Register through `AddWeeklyCheckInModule`; executable hosts remain composition roots.
- Do not add Contracts, Domain, Application Abstractions, or Infrastructure projects unless WeeklyCheckIn gains a proven owned contract, domain type, port, or adapter.

## Tests

- Keep WeeklyCheckIn-owned application tests under `tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests`.
- Keep HTTP, host, architecture, and cross-module scenarios in their central test projects.

## Verification

- Build: `dotnet build Modules/WeeklyCheckIn/Application/FoodDiary.Modules.WeeklyCheckIn.Application.csproj`
- Focused tests: `dotnet test Modules/WeeklyCheckIn/tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests/FoodDiary.Modules.WeeklyCheckIn.Application.Tests.csproj`
- Architecture: `dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

All module projects and tests use `FoodDiary.Modules.WeeklyCheckIn.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
