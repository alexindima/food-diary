# Lessons Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Lessons/`.

## Boundary

- Own lesson queries, progress mutation, administration capabilities, mappings, and models.
- Consume user access and achievement delivery through Application.Abstractions contracts.
- Keep persistence implementations and HTTP transport outside this project.
- Do not reference the legacy `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Lessons/FoodDiary.Application.Lessons.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Lessons`
