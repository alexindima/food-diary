# Admin Application Module Guidelines

## Scope

Rules for `Modules/Admin/Application/`.

## Boundary

- Own administration commands, queries, orchestration services, validation, and admin-facing models.
- Consume feature modules through explicit application-level capabilities and models.
- Consume ReportStatus through ContentReports Domain.Contracts.
- Consume BillingProviderNames through Billing Domain.Contracts and AchievementMetric through Gamification Domain.Contracts; do not reference these aggregate-bearing Domain projects.
- Keep persistence implementations, provider integrations, authorization transport, and HTTP mappings outside this module.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Admin`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

- Register application handlers via AddAdminApplication; hosts use Infrastructure AddAdminModule.
- Keep legacy FoodDiary.Application.Admin AssemblyName and CLR namespaces.

Password reset requests session revocation through Users Contracts IUserSessionRevocationService. Admin must not acquire Identity session repository writes; Identity supplies the existing scoped implementation.

Consume RoleNames through Users Domain.Contracts; do not reference Users Domain for role constants. User mutation remains behind Users capabilities.

Use AchievementDefinitionLimits for validation. Content report mappings accept the owner read model; do not add an aggregate mapping overload.
