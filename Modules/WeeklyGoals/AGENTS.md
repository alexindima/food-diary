# Weekly Goals Logical Module Guidelines

## Scope

Rules for `Modules/WeeklyGoals/`.

## Boundaries

- Own weekly-goal commands, queries, application models, progress calculation, reminder processing, module ports, persistence implementations, and EF configuration.
- Keep the real application assembly at `Application/FoodDiary.Modules.WeeklyGoals.Application.csproj`; do not recreate a root module project or an empty wrapper.
- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddWeeklyGoalsApplication`; composition roots use Infrastructure's `AddWeeklyGoalsModule` facade.
- Read meal activity through Meals Contracts queries via ISender; do not load Meal aggregates.
- Reference Notifications Contracts directly for notification delivery; keep the shared unit of work behind central application contracts.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Keep `WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` in `Domain/FoodDiary.Modules.WeeklyGoals.Domain.csproj` while using canonical module namespaces.
- Keep the module Domain dependency on Users Domain.Contracts for `UserId`; do not add a WeeklyGoals navigation to the `User` aggregate.
- Preserve reminder job ID, cron/options binding, batching, retry, cancellation, and notification behavior.

## Verification

- Build: `dotnet build Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj`
- Focused domain tests: `dotnet test Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests.csproj`
- Focused application tests: `dotnet test Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Application.Tests/FoodDiary.Modules.WeeklyGoals.Application.Tests.csproj`
- Focused infrastructure tests: `dotnet test Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests/FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests.csproj`
- Architecture: `dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Tests

- Keep WeeklyGoals aggregate, identifier, enum, and invariant tests in `Modules/WeeklyGoals/tests/FoodDiary.Modules.WeeklyGoals.Domain.Tests`.
- Keep shared DbContext, migration, HTTP, host, JobManager, architecture, and cross-module scenarios in their central test projects.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

All module projects and tests use `FoodDiary.Modules.WeeklyGoals.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.

The reminder job dispatches SendWeeklyGoalRemindersCommand through ISender. Its handler retains batching, explicit saves and post-commit notifications. GetWeeklyGoal owns read orchestration and reuses WeeklyGoalProgressReader.
