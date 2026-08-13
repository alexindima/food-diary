# Weekly Goals Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.WeeklyGoals/`.

## Boundary

- Own weekly-goal commands, queries, application models, progress calculation, and reminder processing.
- Depend on other business modules only through explicit project references and narrow contracts.
- Meal activity is read through `IMealActivityReadService`; do not load Meal aggregates.
- Notification delivery and persistence remain behind contracts in `FoodDiary.Application.Abstractions`.
- Register the module through `AddWeeklyGoalsModule`; executable hosts remain composition roots.

## Verification

- Build: `dotnet build FoodDiary.Application.WeeklyGoals/FoodDiary.Application.WeeklyGoals.csproj`
- Focused tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~WeeklyGoals`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
