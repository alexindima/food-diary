# Marketing Logical Module Guidelines

## Boundary

- Own attribution commands, queries, conversion state, repository ports, aggregate/id, persistence mapping/adapter, and focused tests.
- Preserve `FoodDiary.Application.Marketing` assembly identity and existing CLR namespaces.
- Keep the Billing-owned `IBillingMarketingConversionRecorder` port in central Application Abstractions; Marketing implements it without exposing persistence contracts.
- Register application behavior through `AddMarketingApplication`; composition roots use Infrastructure's `AddMarketingModule` facade.
- Keep shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Attribution data is private user/session-linked data. Preserve bounded retention, idempotent event IDs, unique conversion constraints, and cancellation-aware cleanup batches.

## Verification

- `dotnet test Modules/Marketing/tests/FoodDiary.Modules.Marketing.Application.Tests/FoodDiary.Modules.Marketing.Application.Tests.csproj`
- `dotnet test Modules/Marketing/tests/FoodDiary.Modules.Marketing.Domain.Tests/FoodDiary.Modules.Marketing.Domain.Tests.csproj`
- `dotnet test Modules/Marketing/tests/FoodDiary.Modules.Marketing.Infrastructure.IntegrationTests/FoodDiary.Modules.Marketing.Infrastructure.IntegrationTests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
