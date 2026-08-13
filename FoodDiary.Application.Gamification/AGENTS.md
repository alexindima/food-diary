# Gamification Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Gamification/`.

## Boundary

- Own achievement evaluation, awarding, reconciliation, administration, and gamification reads.
- Use Meals only through its application-level read capabilities.
- Keep achievement persistence contracts in application abstractions and implementations in infrastructure.
- Do not depend on the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Gamification/FoodDiary.Application.Gamification.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Gamification`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
