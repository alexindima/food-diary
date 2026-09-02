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
- Keep `WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` in `Domain/FoodDiary.Modules.WeeklyGoals.Domain.csproj` while preserving their existing `FoodDiary.Domain.*` CLR namespaces.
- Keep the module Domain dependency on central Domain one-way while `UserId` belongs to Users Domain.Contracts; do not add a WeeklyGoals navigation to the `User` aggregate.
- Preserve legacy `FoodDiary.Application.WeeklyGoals.*`, `FoodDiary.Application.Abstractions.WeeklyGoals.*`, and WeeklyGoals persistence CLR namespaces during this extraction.
- Preserve reminder job ID, cron/options binding, batching, retry, cancellation, and notification behavior.

## Verification

- Build: `dotnet build Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj`
- Focused domain tests: `dotnet test Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests.csproj`
- Focused application tests: `dotnet test Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Application.Tests/FoodDiary.Modules.WeeklyGoals.Application.Tests.csproj`
- Focused infrastructure tests: `dotnet test Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Tests

- Keep WeeklyGoals aggregate, identifier, enum, and invariant tests in `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests`.
- Keep shared DbContext, migration, HTTP, host, JobManager, architecture, and cross-module scenarios in their central test projects.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; central Domain retains shared guards and values without
an aggregate re-export. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
