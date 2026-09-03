# Dietologist Logical Module Guidelines

## Scope

Rules for `Modules/Dietologist/`.

## Boundaries

- Own Dietologist commands, queries, services, policies, module ports, aggregates, persistence adapters, and EF configurations.
- Preserve legacy `FoodDiary.Application.Dietologist.*`, `FoodDiary.Application.Abstractions.Dietologist.*`, `FoodDiary.Domain.*`, and persistence CLR namespaces and the application assembly identity.
- Keep client health data behind relationship authorization and explicit `DietologistPermissions`; no consumer may bypass `IDietologistDashboardAccessService` or the attention-signal projection.
- Register application behavior through `AddDietologistApplication`; composition roots use Infrastructure's `AddDietologistModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Own collaboration audit rule selection in Infrastructure's `CollaborationAuditInterceptor`.
  `AddDietologistModule` registers one scoped EF `ISaveChangesInterceptor` with
  `TryAddEnumerable`; central persistence installs module interceptors after domain
  event dispatch. Preserve synchronous/asynchronous SaveChanges timing and the
  shared transaction. AuditEntry/table/writer stay central; the exact module IVT
  grants internal storage access without making a public audit entity API.
- Focused audit rule/composition tests live in the existing module Infrastructure
  tests. Central PostgreSQL tests retain the shared dispatch/transaction boundary.
- Do not add a Contracts project until a stable cross-module API distinct from adapter-facing application ports is proven.

## Verification

- Application tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/FoodDiary.Modules.Dietologist.Application.Tests.csproj`
- Domain tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/FoodDiary.Modules.Dietologist.Domain.Tests.csproj`
- Infrastructure tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
