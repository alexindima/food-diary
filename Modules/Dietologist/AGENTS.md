# Dietologist Logical Module Guidelines

## Scope

Rules for `Modules/Dietologist/`.

## Boundaries

- Own Dietologist commands, queries, services, policies, module ports, aggregates, persistence adapters, and EF configurations.
- Preserve legacy `FoodDiary.Application.Dietologist.*`, `FoodDiary.Application.Abstractions.Dietologist.*`, `FoodDiary.Domain.*`, and persistence CLR namespaces and the application assembly identity.
- Keep client health data behind relationship authorization and explicit `DietologistPermissions`; no consumer may bypass `IDietologistDashboardAccessService` or the attention-signal projection.
- Register application behavior through `AddDietologistApplication`; composition roots use Infrastructure's `AddDietologistModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Do not add a Contracts project until a stable cross-module API distinct from adapter-facing application ports is proven.

## Verification

- Application tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/FoodDiary.Modules.Dietologist.Application.Tests.csproj`
- Domain tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/FoodDiary.Modules.Dietologist.Domain.Tests.csproj`
- Infrastructure tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
