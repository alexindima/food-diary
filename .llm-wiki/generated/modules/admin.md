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

- Origin: module-graph
- Business-module dependencies: Ai, Authentication, ContentReports, Email, Gamification, Lessons, Users
- Abstraction-contract dependencies: Ai, Audit, Authentication, ContentReports, Email, Lessons, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Integrations, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Admin`
- `FoodDiary.Application/Admin`
- `FoodDiary.Domain/Entities/Admin`
- `FoodDiary.Infrastructure/Persistence/Admin`
- `FoodDiary.Infrastructure/Persistence/Configurations/Admin`
- `FoodDiary.Presentation.Api/Features/Admin`

## HTTP Surface

### AdminAchievementDefinitionsController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminAchievementDefinitionsController.cs`

- `GET /api/v{version:apiVersion}/admin/achievement-definitions`
- `POST /api/v{version:apiVersion}/admin/achievement-definitions`
- `PUT /api/v{version:apiVersion}/admin/achievement-definitions/{id:guid}`

### AdminAcquisitionController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminAcquisitionController.cs`

- `GET /api/v{version:apiVersion}/admin/acquisition/summary`

### AdminAiPromptsController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminAiPromptsController.cs`

- `GET /api/v{version:apiVersion}/admin/ai-prompts`
- `PUT /api/v{version:apiVersion}/admin/ai-prompts/{key:maxlength(64)}/{locale:maxlength(10)}`

### AdminAiUsageController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminAiUsageController.cs`

- `GET /api/v{version:apiVersion}/admin/ai-usage/summary`

### AdminBillingController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminBillingController.cs`

- `GET /api/v{version:apiVersion}/admin/billing/revenue-summary`
- `GET /api/v{version:apiVersion}/admin/billing/subscriptions`
- `GET /api/v{version:apiVersion}/admin/billing/payments`
- `GET /api/v{version:apiVersion}/admin/billing/webhook-events`

### AdminCollaborationAuditController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminCollaborationAuditController.cs`

- `GET /api/v{version:apiVersion}/admin/users/collaboration-audit`

### AdminDashboardController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminDashboardController.cs`

- `GET /api/v{version:apiVersion}/admin/dashboard`

### AdminEmailTemplatesController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminEmailTemplatesController.cs`

- `GET /api/v{version:apiVersion}/admin/email-templates`
- `PUT /api/v{version:apiVersion}/admin/email-templates/{key:maxlength(64)}/{locale:maxlength(10)}`
- `POST /api/v{version:apiVersion}/admin/email-templates/test`

### AdminLessonsController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminLessonsController.cs`

- `GET /api/v{version:apiVersion}/admin/lessons`
- `POST /api/v{version:apiVersion}/admin/lessons`
- `POST /api/v{version:apiVersion}/admin/lessons/import`
- `PUT /api/v{version:apiVersion}/admin/lessons/{id:guid}`
- `DELETE /api/v{version:apiVersion}/admin/lessons/{id:guid}`

### AdminMailInboxController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminMailInboxController.cs`

- `GET /api/v{version:apiVersion}/admin/mail-inbox/messages`
- `GET /api/v{version:apiVersion}/admin/mail-inbox/messages/{id:guid}`
- `POST /api/v{version:apiVersion}/admin/mail-inbox/messages/{id:guid}/read`

### AdminModerationController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminModerationController.cs`

- `GET /api/v{version:apiVersion}/admin/moderation`
- `POST /api/v{version:apiVersion}/admin/moderation/{id:guid}/review`
- `POST /api/v{version:apiVersion}/admin/moderation/{id:guid}/dismiss`

### AdminTelemetryController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminTelemetryController.cs`

- `GET /api/v{version:apiVersion}/admin/telemetry/fasting`

### AdminUserCreationController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminUserCreationController.cs`

- `POST /api/v{version:apiVersion}/admin/users`

### AdminUserPasswordController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminUserPasswordController.cs`

- `PATCH /api/v{version:apiVersion}/admin/users/{id:guid}/password`

### AdminUsersController

Source: `FoodDiary.Presentation.Api/Features/Admin/AdminUsersController.cs`

- `GET /api/v{version:apiVersion}/admin/users`
- `GET /api/v{version:apiVersion}/admin/users/{id:guid}`
- `GET /api/v{version:apiVersion}/admin/users/{id:guid}/role-audit`
- `GET /api/v{version:apiVersion}/admin/users/impersonation-sessions`
- `GET /api/v{version:apiVersion}/admin/users/login-events`
- `GET /api/v{version:apiVersion}/admin/users/login-summary`
- `PATCH /api/v{version:apiVersion}/admin/users/{id:guid}`
- `POST /api/v{version:apiVersion}/admin/users/{id:guid}/impersonation`

### AdminSsoController

Source: `FoodDiary.Presentation.Api/Features/Auth/AdminSsoController.cs`

- `POST /api/v{version:apiVersion}/auth/admin-sso/start`
- `POST /api/v{version:apiVersion}/auth/admin-sso/exchange`

## Boundary Health

- Role: orchestrator
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 28
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 28
- Interfaces: 12
- DTO/read-model/projection types: 10
- Enums: 0
- Exported repository-shaped contracts: 11
- Contracts referencing domain entities: 3
- `class AdminMailInboxErrors`
- `interface IAdminBillingReadRepository`
- `interface IAdminBillingRepository`
- `interface IAdminImpersonationSessionReadRepository`
- `interface IAdminImpersonationSessionRepository`
- `interface IAdminImpersonationSessionWriteRepository`
- `interface IAdminMailInboxReader`
- `interface IAdminUserRoleAuditReadRepository`
- `interface IAdminUserRoleAuditRepository`
- `interface IEmailTemplateReadModelRepository`
- `interface IEmailTemplateReadRepository`
- `interface IEmailTemplateRepository`
- `interface IEmailTemplateWriteRepository`
- `record AdminBillingListFilter`
- `record AdminBillingPaymentReadModel`
- `record AdminBillingRevenueCurrencyReadModel`
- `record AdminBillingRevenueSummaryReadModel`
- `record AdminBillingSubscriptionReadModel`
- `record AdminBillingWebhookEventReadModel`
- `record AdminImpersonationSessionReadModel`
- `record AdminMailInboxMessageDetailsModel`
- `record AdminMailInboxMessageSummaryModel`
- `record AdminUserRoleAuditEventReadModel`
- `record AiUsageBreakdown`
- `record AiUsageDailySummary`
- `record AiUsageSummary`
- `record AiUsageUserSummary`
- `record EmailTemplateReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminFeatureTests.LessonCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminFeatureTests.UserCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminLessonFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/AdminValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/CreateAdminUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/CreateAdminUserCommandValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/GetCollaborationAuditQueryHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Admin/UserLoginActivityFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/AdminInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Authentication/AdminSsoServiceTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AdminAchievementDefinitionsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AdminControllersCoverageTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AdminHttpMappingsTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/AdminSsoControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
