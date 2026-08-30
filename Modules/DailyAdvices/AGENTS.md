# Daily Advices Logical Module Guidelines

## Boundary

- Own `DailyAdvice`, `DailyAdviceId`, selection, queries, persistence ports, EF configuration, and repository behavior.
- Keep the real application assembly at `Application/FoodDiary.Modules.DailyAdvices.Application.csproj`; do not create a root wrapper or an unproven Contracts project.
- Preserve legacy CLR namespaces and the application assembly name.
- Read persistence projections through `IDailyAdviceReadModelRepository`; do not expose aggregates to Dashboard.
- Register application behavior through `AddDailyAdvicesApplication`; composition roots use Infrastructure's `AddDailyAdvicesModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Dashboard may consume the mediator query/model surface, but not repository ports, Domain, or Infrastructure.

## Verification

- `dotnet test Modules/DailyAdvices/tests/FoodDiary.Modules.DailyAdvices.Application.Tests/FoodDiary.Modules.DailyAdvices.Application.Tests.csproj`
- `dotnet test Modules/DailyAdvices/tests/FoodDiary.Modules.DailyAdvices.Domain.Tests/FoodDiary.Modules.DailyAdvices.Domain.Tests.csproj`
- `dotnet test Modules/DailyAdvices/tests/FoodDiary.Modules.DailyAdvices.Infrastructure.Tests/FoodDiary.Modules.DailyAdvices.Infrastructure.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
