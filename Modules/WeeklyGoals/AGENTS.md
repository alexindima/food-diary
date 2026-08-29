# Weekly Goals Logical Module Guidelines

## Scope

Rules for `Modules/WeeklyGoals/`.

## Boundaries

- Own weekly-goal commands, queries, application models, progress calculation, reminder processing, module ports, persistence implementations, and EF configuration.
- Keep the real application assembly at `Application/FoodDiary.Modules.WeeklyGoals.Application.csproj`; do not recreate a root module project or an empty wrapper.
- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddWeeklyGoalsApplication`; composition roots use Infrastructure's `AddWeeklyGoalsModule` facade.
- Read meal activity only through `IMealActivityReadService`; do not load Meal aggregates.
- Keep notification delivery and the shared unit of work behind central application contracts.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Keep `WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` in central Domain as a CLR and EF compatibility seam.
- Preserve legacy `FoodDiary.Application.WeeklyGoals.*`, `FoodDiary.Application.Abstractions.WeeklyGoals.*`, and WeeklyGoals persistence CLR namespaces during this extraction.
- Preserve reminder job ID, cron/options binding, batching, retry, cancellation, and notification behavior.

## Verification

- Build: `dotnet build Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj`
- Focused application tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~WeeklyGoals`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
