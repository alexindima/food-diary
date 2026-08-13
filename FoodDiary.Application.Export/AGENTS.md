# Export Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Export/`.

## Boundary

- Own diary and cycle export queries, validation, file-result models, and CSV generation.
- Consume Cycles and Meals only through their application-level read capabilities.
- Keep PDF rendering implementations and HTTP transport outside this module.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Export/FoodDiary.Application.Export.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Export`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
