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
- Business-module dependencies: Ai, BodyMetrics, ContentReports, DailyAdvices, Gamification, Identity, Lessons, Users
- Abstraction-contract dependencies: Ai, BodyMetrics, ContentReports, DailyAdvices, Gamification, Identity, Lessons, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Admin/Application`
- `Modules/Admin/Application.Abstractions`
- `Modules/Admin/Contracts`
- `Modules/Admin/Domain`
- `Modules/Admin/Infrastructure`
- `Modules/Admin/Infrastructure/Integrations/MailInbox`
- `Modules/Admin/PersistenceModel`
- `Modules/Admin/Presentation`

## HTTP Surface

### AdminAchievementDefinitionsController

Source: `Modules/Admin/Presentation/Controllers/AdminAchievementDefinitionsController.cs`

- `GET /api/v{version:apiVersion}/admin/achievement-definitions`
- `POST /api/v{version:apiVersion}/admin/achievement-definitions`
- `PUT /api/v{version:apiVersion}/admin/achievement-definitions/{id:guid}`

### AdminAcquisitionController

Source: `Modules/Admin/Presentation/Controllers/AdminAcquisitionController.cs`

- `GET /api/v{version:apiVersion}/admin/acquisition/range`
- `GET /api/v{version:apiVersion}/admin/acquisition/summary`

### AdminAiPromptsController

Source: `Modules/Admin/Presentation/Controllers/AdminAiPromptsController.cs`

- `GET /api/v{version:apiVersion}/admin/ai-prompts/scenarios`
- `POST /api/v{version:apiVersion}/admin/ai-prompts/preview`
- `POST /api/v{version:apiVersion}/admin/ai-prompts/test`
- `GET /api/v{version:apiVersion}/admin/ai-prompts`
- `GET /api/v{version:apiVersion}/admin/ai-prompts/{key:maxlength(64)}/{locale:maxlength(10)}/revisions`
- `PUT /api/v{version:apiVersion}/admin/ai-prompts/{key:maxlength(64)}/{locale:maxlength(10)}`

### AdminAiUsageController

Source: `Modules/Admin/Presentation/Controllers/AdminAiUsageController.cs`

- `GET /api/v{version:apiVersion}/admin/ai-usage/summary`

### AdminAuditController

Source: `Modules/Admin/Presentation/Controllers/AdminAuditController.cs`

- `GET /api/v{version:apiVersion}/admin/audit`

### AdminBillingController

Source: `Modules/Admin/Presentation/Controllers/AdminBillingController.cs`

- `GET /api/v{version:apiVersion}/admin/billing/cancellations`
- `GET /api/v{version:apiVersion}/admin/billing/revenue-summary`
- `GET /api/v{version:apiVersion}/admin/billing/subscriptions`
- `GET /api/v{version:apiVersion}/admin/billing/payments`
- `GET /api/v{version:apiVersion}/admin/billing/webhook-events`

### AdminBugReportsController

Source: `Modules/Admin/Presentation/Controllers/AdminBugReportsController.cs`

- `GET /api/v{version:apiVersion}/admin/bugs`
- `GET /api/v{version:apiVersion}/admin/bugs/journal`

### AdminCollaborationAuditController

Source: `Modules/Admin/Presentation/Controllers/AdminCollaborationAuditController.cs`

- `GET /api/v{version:apiVersion}/admin/users/collaboration-audit`

### AdminDailyAdvicesController

Source: `Modules/Admin/Presentation/Controllers/AdminDailyAdvicesController.cs`

- `GET /api/v{version:apiVersion}/admin/daily-advices`
- `POST /api/v{version:apiVersion}/admin/daily-advices/import`
- `GET /api/v{version:apiVersion}/admin/daily-advices/groups`
- `POST /api/v{version:apiVersion}/admin/daily-advices/groups/import`
- `PUT /api/v{version:apiVersion}/admin/daily-advices/groups/{id:guid}`
- `DELETE /api/v{version:apiVersion}/admin/daily-advices/groups/{id:guid}`

### AdminDashboardController

Source: `Modules/Admin/Presentation/Controllers/AdminDashboardController.cs`

- `GET /api/v{version:apiVersion}/admin/dashboard/overview`
- `GET /api/v{version:apiVersion}/admin/dashboard`

### AdminEmailTemplatesController

Source: `Modules/Admin/Presentation/Controllers/AdminEmailTemplatesController.cs`

- `GET /api/v{version:apiVersion}/admin/email-templates`
- `GET /api/v{version:apiVersion}/admin/email-templates/{key:maxlength(64)}/{locale:maxlength(10)}/revisions`
- `PUT /api/v{version:apiVersion}/admin/email-templates/{key:maxlength(64)}/{locale:maxlength(10)}`
- `POST /api/v{version:apiVersion}/admin/email-templates/test`

### AdminLessonsController

Source: `Modules/Admin/Presentation/Controllers/AdminLessonsController.cs`

- `GET /api/v{version:apiVersion}/admin/lessons`
- `POST /api/v{version:apiVersion}/admin/lessons`
- `POST /api/v{version:apiVersion}/admin/lessons/import`
- `PUT /api/v{version:apiVersion}/admin/lessons/{id:guid}`
- `DELETE /api/v{version:apiVersion}/admin/lessons/{id:guid}`

### AdminMailInboxController

Source: `Modules/Admin/Presentation/Controllers/AdminMailInboxController.cs`

- `GET /api/v{version:apiVersion}/admin/mail-inbox/messages/page`
- `GET /api/v{version:apiVersion}/admin/mail-inbox/messages`
- `GET /api/v{version:apiVersion}/admin/mail-inbox/messages/{id:guid}`
- `POST /api/v{version:apiVersion}/admin/mail-inbox/messages/{id:guid}/read`

### AdminModerationController

Source: `Modules/Admin/Presentation/Controllers/AdminModerationController.cs`

- `GET /api/v{version:apiVersion}/admin/moderation`
- `POST /api/v{version:apiVersion}/admin/moderation/{id:guid}/review`
- `POST /api/v{version:apiVersion}/admin/moderation/{id:guid}/dismiss`

### AdminOutgoingEmailsController

Source: `Modules/Admin/Presentation/Controllers/AdminOutgoingEmailsController.cs`

- `GET /api/v{version:apiVersion}/admin/outgoing-emails`

### AdminRetentionController

Source: `Modules/Admin/Presentation/Controllers/AdminRetentionController.cs`

- `GET /api/v{version:apiVersion}/admin/analytics/retention`

### AdminTelemetryController

Source: `Modules/Admin/Presentation/Controllers/AdminTelemetryController.cs`

- `GET /api/v{version:apiVersion}/admin/telemetry/fasting`

### AdminUserCreationController

Source: `Modules/Admin/Presentation/Controllers/AdminUserCreationController.cs`

- `POST /api/v{version:apiVersion}/admin/users`

### AdminUserPasswordController

Source: `Modules/Admin/Presentation/Controllers/AdminUserPasswordController.cs`

- `PATCH /api/v{version:apiVersion}/admin/users/{id:guid}/password`

### AdminUserPremiumGrantController

Source: `Modules/Admin/Presentation/Controllers/AdminUserPremiumGrantController.cs`

- `GET /api/v{version:apiVersion}/admin/users/{id:guid}/premium-grant`
- `PUT /api/v{version:apiVersion}/admin/users/{id:guid}/premium-grant`
- `DELETE /api/v{version:apiVersion}/admin/users/{id:guid}/premium-grant`

### AdminUsersController

Source: `Modules/Admin/Presentation/Controllers/AdminUsersController.cs`

- `GET /api/v{version:apiVersion}/admin/users`
- `GET /api/v{version:apiVersion}/admin/users/{id:guid}`
- `GET /api/v{version:apiVersion}/admin/users/{id:guid}/role-audit`
- `GET /api/v{version:apiVersion}/admin/users/impersonation-sessions`
- `GET /api/v{version:apiVersion}/admin/users/login-events`
- `GET /api/v{version:apiVersion}/admin/users/login-summary`
- `PATCH /api/v{version:apiVersion}/admin/users/{id:guid}`
- `POST /api/v{version:apiVersion}/admin/users/{id:guid}/impersonation`

## Boundary Health

- Role: orchestrator
- Physical isolation: module-root
- Architecture guardrails: assembly-isolated
- Declared owned entities: AdminImpersonationSession
- Public contract files: 44
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 44
- Interfaces: 13
- DTO/read-model/projection types: 15
- Enums: 0
- Exported repository-shaped contracts: 3
- Contracts referencing domain entities: 0
- `class AdminMailInboxErrors`
- `class ImpersonationErrors`
- `interface IAdminBillingQuery`
- `interface IAdminBillingRepository`
- `interface IAdminBugReportReader`
- `interface IAdminDashboardMetricsReader`
- `interface IAdminImpersonationHandoffService`
- `interface IAdminImpersonationSessionQuery`
- `interface IAdminImpersonationSessionWriteRepository`
- `interface IAdminMailInboxReader`
- `interface IAdminRetentionReader`
- `interface IAdminUserRoleAuditQuery`
- `interface IAdminUserRoleAuditRepository`
- `interface IBugAcknowledgementReceipts`
- `interface IBugAcknowledgementSource`
- `record AdminAuditRunSummary`
- `record AdminBillingCancellationReadModel`
- `record AdminBillingCancellationReasonReadModel`
- `record AdminBillingCancellationReportReadModel`
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
- ... 14 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/AdminAiPromptWorkbenchIntegrationTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/AdminMailTransportTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/AdminNotificationTransportTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/AdminUserPremiumGrantApiTests.cs`
- [behavioral-or-text-match] `Hosts/tests/FoodDiary.Web.Api.Tests/Extensions/AdminProxyOriginTests.cs`
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
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/AdminWorkbenchDispatchTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/ContentReportDispatchTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/CreateAdminUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/CreateAdminUserCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/ExchangeAdminImpersonationTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/GetAdminUsersQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/GetCollaborationAuditQueryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserLoginActivityFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/AdminBillingCancellationQueryTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Authentication/SecretInputLimitValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/FeatureErrorContractTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
