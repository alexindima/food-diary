# Admin Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Admin/`.

## Boundary

- Own administration commands, queries, orchestration services, validation, and admin-facing models.
- Consume feature modules through explicit application-level capabilities and models.
- Keep persistence implementations, provider integrations, authorization transport, and HTTP mappings outside this module.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Admin/FoodDiary.Application.Admin.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Admin`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
