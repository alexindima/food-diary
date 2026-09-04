# Admin Application Module Guidelines

## Scope

Rules for `Modules/Admin/Application/`.

## Boundary

- Own administration commands, queries, orchestration services, validation, and admin-facing models.
- Consume feature modules through explicit application-level capabilities and models.
- Reference ContentReports Domain directly for ReportStatus used by the existing moderation read contracts. This enum dependency does not authorize acquiring aggregate or persistence capabilities.
- Reference Billing Domain directly for the existing BillingProviderNames constants used by AdminBillingQueryFilters; const inlining hides this compile dependency from emitted assembly references. This does not authorize Billing aggregate writes.
- Keep persistence implementations, provider integrations, authorization transport, and HTTP mappings outside this module.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Admin`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

- Register application handlers via AddAdminApplication; hosts use Infrastructure AddAdminModule.
- Keep legacy FoodDiary.Application.Admin AssemblyName and CLR namespaces.
