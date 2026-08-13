# Dashboard Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Dashboard/`.

## Boundary

- Own dashboard snapshot composition, dashboard queries, validation, and dashboard-specific models.
- Consume extracted feature modules only through their application-level read capabilities and mediator requests.
- Keep persistence implementations and HTTP transport outside this module.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Dashboard/FoodDiary.Application.Dashboard.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Dashboard`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
