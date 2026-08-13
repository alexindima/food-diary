# Daily Advices Application Module Guidelines

## Boundary

- Own daily-advice selection, queries, and application models.
- Read persistence projections through `IDailyAdviceReadModelRepository`; do not expose `DailyAdvice` aggregates.
- Register through `AddDailyAdvicesModule`; hosts remain composition roots.

## Verification

- `dotnet build FoodDiary.Application.DailyAdvices/FoodDiary.Application.DailyAdvices.csproj`
- `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~DailyAdvices`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
