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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Admin/Application`
- `Modules/Admin/Application/Abstractions`
- `Modules/Admin/Contracts`
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
- Public contract files: 39
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 39
- Interfaces: 14
- DTO/read-model/projection types: 12
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 1
- `class AdminMailInboxErrors`
- `interface IAdminBillingReadRepository`
- `interface IAdminBillingRepository`
- `interface IAdminBugReportReader`
- `interface IAdminDashboardMetricsReader`
- `interface IAdminImpersonationHandoffService`
- `interface IAdminImpersonationSessionReadRepository`
- `interface IAdminImpersonationSessionRepository`
- `interface IAdminImpersonationSessionWriteRepository`
- `interface IAdminMailInboxReader`
- `interface IAdminRetentionReader`
- `interface IAdminUserRoleAuditReadRepository`
- `interface IAdminUserRoleAuditRepository`
- `interface IBugAcknowledgementReceipts`
- `interface IBugAcknowledgementSource`
- `record AdminBillingListFilter`
- `record AdminBillingPaymentReadModel`
- `record AdminBillingRevenueCurrencyReadModel`
- `record AdminBillingRevenueSummaryReadModel`
- `record AdminBillingSubscriptionReadModel`
- `record AdminBillingWebhookEventReadModel`
- `record AdminBugReportEntry`
- `record AdminBugReportFilter`
- `record AdminBugReportPage`
- `record AdminDashboardMetrics`
- `record AdminDashboardRevenuePoint`
- `record AdminDashboardTrend`
- `record AdminImpersonationSessionReadModel`
- `record AdminMailInboxDmarcRecordModel`
- `record AdminMailInboxDmarcReportModel`
- ... 9 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminAchievementDefinitionHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminDashboardOverviewTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.DefaultMailReaderTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.LessonCommandTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.UserCommandTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminJournalQueryTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminJournalValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminLessonFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminTemplateHistoryTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/CreateAdminUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/CreateAdminUserCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/ExchangeAdminImpersonationTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/GetAdminUsersQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/GetCollaborationAuditQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserLoginActivityFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/BugAcknowledgementServiceTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Domain.Tests/Domain/AdminInvariantTests.cs`
- [integration] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.IntegrationTests/Integration/AdminDashboardMetricsIntegrationTests.cs`
- [integration] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.IntegrationTests/Integration/AdminRetentionIntegrationTests.cs`
- [integration] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.IntegrationTests/Integration/AdminUserRoleAuditRepositoryIntegrationTests.cs`
- [integration] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.IntegrationTests/Integration/BugAcknowledgementReceiptIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.Tests/AdminPersistenceRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Infrastructure.Tests/Authentication/AdminImpersonationHandoffServiceTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
