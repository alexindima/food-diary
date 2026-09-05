---
id: generated.module.admin
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Admin

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Ai, Audit, Authentication, Email, Lessons, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Modules.Identity.Presentation, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Admin/Application`
- `Modules/Admin/Application/Abstractions`
- `Modules/Admin/Domain`
- `Modules/Admin/Infrastructure`
- `Modules/Admin/Infrastructure/Integrations/MailInbox`
- `Modules/Admin/Infrastructure/Model`
- `Modules/Admin/Presentation`
- `Modules/Admin/Presentation/Features/Admin`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: orchestrator
- Physical isolation: module-root
- Architecture guardrails: assembly-isolated
- Declared owned entities: AdminImpersonationSession
- Public contract files: 22
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 22
- Interfaces: 9
- DTO/read-model/projection types: 11
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 1
- `class AdminMailInboxErrors`
- `interface IAdminBillingReadRepository`
- `interface IAdminBillingRepository`
- `interface IAdminImpersonationHandoffService`
- `interface IAdminImpersonationSessionReadRepository`
- `interface IAdminImpersonationSessionRepository`
- `interface IAdminImpersonationSessionWriteRepository`
- `interface IAdminMailInboxReader`
- `interface IAdminUserRoleAuditReadRepository`
- `interface IAdminUserRoleAuditRepository`
- `record AdminBillingListFilter`
- `record AdminBillingPaymentReadModel`
- `record AdminBillingRevenueCurrencyReadModel`
- `record AdminBillingRevenueSummaryReadModel`
- `record AdminBillingSubscriptionReadModel`
- `record AdminBillingWebhookEventReadModel`
- `record AdminImpersonationSessionReadModel`
- `record AdminMailInboxDmarcRecordModel`
- `record AdminMailInboxDmarcReportModel`
- `record AdminMailInboxMessageDetailsModel`
- `record AdminMailInboxMessageSummaryModel`
- `record AdminUserRoleAuditEventReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminAchievementDefinitionHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.LessonCommandTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.UserCommandTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminLessonFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/CreateAdminUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/CreateAdminUserCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/GetAdminUsersQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/GetCollaborationAuditQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserLoginActivityFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Domain.Tests/Domain/AdminInvariantTests.cs`
- [integration] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.IntegrationTests/Integration/AdminUserRoleAuditRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.Tests/AdminPersistenceRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.Tests/Authentication/AdminImpersonationHandoffServiceTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.Tests/Integrations/MailInboxAdminReaderTests.cs`
- [presentation] `Modules/Admin/tests/FoodDiary.Modules.Admin.Presentation.Tests/AdminAchievementDefinitionsControllerTests.cs`
- [presentation] `Modules/Admin/tests/FoodDiary.Modules.Admin.Presentation.Tests/AdminControllersCoverageTests.cs`
- [presentation] `Modules/Admin/tests/FoodDiary.Modules.Admin.Presentation.Tests/AdminHttpMappingsTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.Tests/Authentication/AdminSsoProtocolTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.Tests/Authentication/AdminSsoServiceTests.cs`
- [presentation] `Modules/Identity/tests/FoodDiary.Modules.Identity.Presentation.Tests/AdminSsoControllerTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/AdminModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
