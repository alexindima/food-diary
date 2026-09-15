# Daily Advices Logical Module Guidelines

## Boundary

- Own `DailyAdvice`, `DailyAdviceId`, selection, queries, persistence ports, EF configuration, and repository behavior.
- Keep the real application assembly at `Application/FoodDiary.Modules.DailyAdvices.Application.csproj`; Contracts owns the public query/model surface. Do not create a root wrapper.
- Use canonical `FoodDiary.Modules.DailyAdvices.<Project>` assembly identities and folder-aligned namespaces.
- Read persistence projections through `IDailyAdviceReadModelRepository`; do not expose aggregates to Dashboard.
- Register application behavior through `AddDailyAdvicesApplication`; composition roots use Infrastructure's `AddDailyAdvicesModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Dashboard may consume the mediator query/model surface, but not repository ports, Domain, or Infrastructure.

## Verification

- `dotnet test Modules/DailyAdvices/tests/FoodDiary.Modules.DailyAdvices.Application.Tests/FoodDiary.Modules.DailyAdvices.Application.Tests.csproj`
- `dotnet test Modules/DailyAdvices/tests/FoodDiary.Modules.DailyAdvices.Domain.Tests/FoodDiary.Modules.DailyAdvices.Domain.Tests.csproj`
- `dotnet test Modules/DailyAdvices/tests/FoodDiary.Modules.DailyAdvices.Infrastructure.Tests/FoodDiary.Modules.DailyAdvices.Infrastructure.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Consumer boundary

Own DailyAdviceModel and GetDailyAdviceQuery consumed by Dashboard. Keep generation, persistence and query handlers in Application. See `Contracts/AGENTS.md` and ADR 0033.

DailyAdvices owns a single-entity runtime context for its no-tracking advice
projections. Initializer seeding and historical migrations remain central;
locale normalization and result ordering are unchanged (ADR 0040).

## Refactoring guardrails

- Keep all projects as siblings; namespace and physical layout are checked by MigratedModuleNamespaceTests and PhysicalProjectLayoutTests.
- Put single-use query orchestration directly in its handler; retain only genuinely shared operations, independent algorithms, authorization capabilities and technical ports.
