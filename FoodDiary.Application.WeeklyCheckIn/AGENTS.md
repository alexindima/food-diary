# Weekly Check-In Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.WeeklyCheckIn/`.

## Boundary

- Own weekly check-in queries, models, calculations, and user-profile composition.
- Read hydration and meal activity only through their public application contracts.
- Do not load Hydration or Meal aggregates.
- Register through `AddWeeklyCheckInModule`; executable hosts remain composition roots.

## Verification

- Build: `dotnet build FoodDiary.Application.WeeklyCheckIn/FoodDiary.Application.WeeklyCheckIn.csproj`
- Focused tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~WeeklyCheckIn`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
