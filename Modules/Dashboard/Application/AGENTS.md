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

Reference Products FoodQuality directly for the existing shared food-quality calculation. Do not reference Products Domain for scoring.

`IDashboardStatisticsReadService` must be supplied by composition (normally Dashboard Infrastructure). Never register a mediator fallback that sends GetStatisticsQuery back to the handler using the same reader. Dashboard consumes its own statistics read models and does not reference Statistics.Application.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.
