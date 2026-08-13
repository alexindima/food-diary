# Statistics Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Statistics/`.

## Boundary

- Own statistics queries, summary composition, response models, and date-range normalization.
- Consume dashboard, body-metric, and user capabilities only through Application.Abstractions contracts.
- Keep persistence implementations and HTTP transport outside this project.
- Do not reference the legacy `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Statistics/FoodDiary.Application.Statistics.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Statistics`
