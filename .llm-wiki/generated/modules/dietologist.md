---
id: generated.module.dietologist
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Dietologist

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Dietologist/Application/FoodDiary.Modules.Dietologist.Application.csproj`
- Business-module dependencies: Identity, Notifications, Users
- Abstraction-contract dependencies: Identity, Notifications, Users
- Business-module consumers: Dashboard
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Dietologist/Application`
- `Modules/Dietologist/Application.Abstractions`
- `Modules/Dietologist/Domain`
- `Modules/Dietologist/Infrastructure`
- `Modules/Dietologist/PersistenceModel`
- `Modules/Dietologist/Presentation`

## HTTP Surface

### BulkRecommendationsController

Source: `Modules/Dietologist/Presentation/Controllers/BulkRecommendationsController.cs`

- `POST /api/v{version:apiVersion}/dietologist/recommendations/bulk`

### ClientTasksController

Source: `Modules/Dietologist/Presentation/Controllers/ClientTasksController.cs`

- `GET /api/v{version:apiVersion}/client-tasks`
- `PUT /api/v{version:apiVersion}/client-tasks/{taskId:guid}/status`

### DietologistAttentionController

Source: `Modules/Dietologist/Presentation/Controllers/DietologistAttentionController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients/attention`
- `PUT /api/v{version:apiVersion}/dietologist/clients/attention/{signalId}/state`

### DietologistClientTasksController

Source: `Modules/Dietologist/Presentation/Controllers/DietologistClientTasksController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/tasks`
- `POST /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/tasks`
- `PUT /api/v{version:apiVersion}/dietologist/clients/tasks/{taskId:guid}/cancel`

### DietologistClientsController

Source: `Modules/Dietologist/Presentation/Controllers/DietologistClientsController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients`
- `DELETE /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/dashboard`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/goals`
- `POST /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/recommendations`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/recommendations`

### DietologistController

Source: `Modules/Dietologist/Presentation/Controllers/DietologistController.cs`

- `POST /api/v{version:apiVersion}/dietologist/invite`
- `DELETE /api/v{version:apiVersion}/dietologist/relationship`
- `PUT /api/v{version:apiVersion}/dietologist/permissions`
- `GET /api/v{version:apiVersion}/dietologist/my-dietologist`
- `GET /api/v{version:apiVersion}/dietologist/relationship`

### DietologistInvitationsController

Source: `Modules/Dietologist/Presentation/Controllers/DietologistInvitationsController.cs`

- `POST /api/v{version:apiVersion}/dietologist/accept`
- `POST /api/v{version:apiVersion}/dietologist/decline`
- `GET /api/v{version:apiVersion}/dietologist/invitations/{invitationId:guid}/current-user`
- `POST /api/v{version:apiVersion}/dietologist/invitations/{invitationId:guid}/accept-current-user`
- `POST /api/v{version:apiVersion}/dietologist/invitations/{invitationId:guid}/decline-current-user`
- `GET /api/v{version:apiVersion}/dietologist/invitation/{invitationId:guid}`

### RecommendationTemplatesController

Source: `Modules/Dietologist/Presentation/Controllers/RecommendationTemplatesController.cs`

- `GET /api/v{version:apiVersion}/dietologist/recommendation-templates`
- `POST /api/v{version:apiVersion}/dietologist/recommendation-templates`
- `PUT /api/v{version:apiVersion}/dietologist/recommendation-templates/{templateId:guid}`
- `DELETE /api/v{version:apiVersion}/dietologist/recommendation-templates/{templateId:guid}`

### RecommendationsController

Source: `Modules/Dietologist/Presentation/Controllers/RecommendationsController.cs`

- `GET /api/v{version:apiVersion}/recommendations`
- `PUT /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/read`
- `GET /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/comments`
- `POST /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/comments`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ClientTask, DietologistInvitation, Recommendation, RecommendationBulkDispatch, RecommendationComment, RecommendationTemplate
- Public contract files: 34
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 34
- Interfaces: 22
- DTO/read-model/projection types: 10
- Enums: 0
- Exported repository-shaped contracts: 20
- Contracts referencing domain entities: 0
- `class DietologistErrors`
- `interface IAttentionSignalMetricsReadService`
- `interface IClientTaskReadModelRepository`
- `interface IClientTaskRepository`
- `interface IClientTaskWriteRepository`
- `interface IDietologistEmailSender`
- `interface IDietologistInvitationReadModelRepository`
- `interface IDietologistInvitationReadRepository`
- `interface IDietologistInvitationRepository`
- `interface IDietologistInvitationWriteRepository`
- `interface IRecommendationBulkDispatchLookupRepository`
- `interface IRecommendationBulkDispatchRepository`
- `interface IRecommendationBulkDispatchWriteRepository`
- `interface IRecommendationCommentReadModelRepository`
- `interface IRecommendationCommentRepository`
- `interface IRecommendationCommentWriteRepository`
- `interface IRecommendationReadModelRepository`
- `interface IRecommendationReadRepository`
- `interface IRecommendationRepository`
- `interface IRecommendationTemplateReadModelRepository`
- `interface IRecommendationTemplateRepository`
- `interface IRecommendationTemplateWriteRepository`
- `interface IRecommendationWriteRepository`
- `record AttentionSignalDailyCaloriesReadModel`
- `record AttentionSignalMetricsReadModel`
- `record AttentionSignalWeightPointReadModel`
- `record ClientTaskReadModel`
- `record DietologistInvitationMessage`
- `record DietologistInvitationReadModel`
- `record RecommendationBulkDispatchReadModel`
- ... 4 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationCurrentUserFlowTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationNotificationIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/AttentionSignalTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Authentication/SecretInputLimitValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/BulkCreateRecommendationsHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/ClientTaskDueReminderProcessorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/ClientTaskHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DefaultClientTaskPaginationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DefaultCommentPaginationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DefaultTemplatePaginationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistAccessPolicyTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistClientListBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.AcceptInvitationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.ClientTaskAccessTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.CommentAccessSecurityTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.DeclineInvitationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.InvitationVerificationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.InviteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.ProfileFixtures.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.RecommendationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.RelationshipCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistInvitationTokenGeneratorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistNotificationContractTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistProfileDisplayNameTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistReadFailureTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistResidualCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistValidatorTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
