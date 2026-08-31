# Dashboard Application Module Guidelines

## Scope

Rules for `Modules/Dashboard/Application/`.

## Boundary

- Own dashboard snapshot composition, dashboard queries, validation, and dashboard-specific models.
- Consume extracted feature modules only through their application-level read capabilities and mediator requests.
- Keep persistence implementations and HTTP transport outside this module.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build Modules/Dashboard/Application/FoodDiary.Modules.Dashboard.Application.csproj`
- Tests: `dotnet test Modules/Dashboard/tests/FoodDiary.Modules.Dashboard.Application.Tests/FoodDiary.Modules.Dashboard.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
