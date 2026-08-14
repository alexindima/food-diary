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
- Extracted project: `FoodDiary.Application.Dietologist/FoodDiary.Application.Dietologist.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Dietologist`
- `FoodDiary.Domain/Entities/Dietologist`
- `FoodDiary.Infrastructure/Persistence/Configurations/Dietologist`
- `FoodDiary.Infrastructure/Persistence/Dietologist`
- `FoodDiary.Presentation.Api/Features/Dietologist`

## HTTP Surface

### BulkRecommendationsController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/BulkRecommendationsController.cs`

- `POST /api/v{version:apiVersion}/dietologist/recommendations/bulk`

### ClientTasksController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/ClientTasksController.cs`

- `GET /api/v{version:apiVersion}/client-tasks`
- `PUT /api/v{version:apiVersion}/client-tasks/{taskId:guid}/status`

### DietologistAttentionController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistAttentionController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients/attention`
- `PUT /api/v{version:apiVersion}/dietologist/clients/attention/{signalId}/state`

### DietologistClientTasksController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistClientTasksController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/tasks`
- `POST /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/tasks`
- `PUT /api/v{version:apiVersion}/dietologist/clients/tasks/{taskId:guid}/cancel`

### DietologistClientsController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistClientsController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients`
- `DELETE /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/dashboard`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/goals`
- `POST /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/recommendations`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/recommendations`

### DietologistController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistController.cs`

- `POST /api/v{version:apiVersion}/dietologist/invite`
- `DELETE /api/v{version:apiVersion}/dietologist/relationship`
- `PUT /api/v{version:apiVersion}/dietologist/permissions`
- `GET /api/v{version:apiVersion}/dietologist/my-dietologist`
- `GET /api/v{version:apiVersion}/dietologist/relationship`

### DietologistInvitationsController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistInvitationsController.cs`

- `POST /api/v{version:apiVersion}/dietologist/accept`
- `POST /api/v{version:apiVersion}/dietologist/decline`
- `GET /api/v{version:apiVersion}/dietologist/invitations/{invitationId:guid}/current-user`
- `POST /api/v{version:apiVersion}/dietologist/invitations/{invitationId:guid}/accept-current-user`
- `POST /api/v{version:apiVersion}/dietologist/invitations/{invitationId:guid}/decline-current-user`
- `GET /api/v{version:apiVersion}/dietologist/invitation/{invitationId:guid}`

### RecommendationTemplatesController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/RecommendationTemplatesController.cs`

- `GET /api/v{version:apiVersion}/dietologist/recommendation-templates`
- `POST /api/v{version:apiVersion}/dietologist/recommendation-templates`
- `PUT /api/v{version:apiVersion}/dietologist/recommendation-templates/{templateId:guid}`
- `DELETE /api/v{version:apiVersion}/dietologist/recommendation-templates/{templateId:guid}`

### RecommendationsController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/RecommendationsController.cs`

- `GET /api/v{version:apiVersion}/recommendations`
- `PUT /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/read`
- `GET /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/comments`
- `POST /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/comments`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: not yet enumerated
- Public contract files: 35
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 35
- Interfaces: 23
- DTO/read-model/projection types: 10
- Enums: 0
- Exported repository-shaped contracts: 20
- Contracts referencing domain entities: 8
- `class DietologistErrors`
- `interface IAttentionSignalMetricsReadService`
- `interface IClientTaskReadModelRepository`
- `interface IClientTaskRepository`
- `interface IClientTaskWriteRepository`
- `interface IDietologistDashboardAccessService`
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
- ... 5 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/AttentionSignalTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/BulkCreateRecommendationsHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/ClientTaskDueReminderProcessorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/ClientTaskHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistAccessPolicyTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.AcceptInvitationCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.DeclineInvitationCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.InviteCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.RecommendationCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.RelationshipCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistInvitationTokenGeneratorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistResidualCoverageTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/DietologistValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/RecommendationCommentHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Dietologist/RecommendationTemplateHandlerTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/DietologistModuleBoundaryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/DietologistInvitationInvariantTests.cs`
- [integration] `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/DietologistPersistenceIntegrationTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/DietologistEmailSenderTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DietologistClientsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DietologistControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DietologistHttpMappingsTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DietologistInvitationsControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/DietologistNewEndpointsCoverageTests.cs`
- [integration] `tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationCurrentUserFlowTests.cs`
- [integration] `tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationNotificationIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
