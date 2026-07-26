---
id: generated.module.dietologist
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Dietologist

## Graph

- Origin: module-graph
- Dependencies: Authentication, Dashboard, Notifications, Users
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Dietologist`
- `FoodDiary.Application/Dietologist`
- `FoodDiary.Domain/Entities/Dietologist`
- `FoodDiary.Infrastructure/Persistence/Configurations/Dietologist`
- `FoodDiary.Infrastructure/Persistence/Dietologist`
- `FoodDiary.Presentation.Api/Features/Dietologist`
- `FoodDiary.Web.Client/src/app/features/dietologist`
- `tests/FoodDiary.Application.Tests/Dietologist`

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

### DietologistClientsController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistClientsController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients`
- `DELETE /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/dashboard`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/goals`
- `POST /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/recommendations`
- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/recommendations`

### DietologistClientTasksController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/DietologistClientTasksController.cs`

- `GET /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/tasks`
- `POST /api/v{version:apiVersion}/dietologist/clients/{clientUserId:guid}/tasks`
- `PUT /api/v{version:apiVersion}/dietologist/clients/tasks/{taskId:guid}/cancel`

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

### RecommendationsController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/RecommendationsController.cs`

- `GET /api/v{version:apiVersion}/recommendations`
- `PUT /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/read`
- `GET /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/comments`
- `POST /api/v{version:apiVersion}/recommendations/{recommendationId:guid}/comments`

### RecommendationTemplatesController

Source: `FoodDiary.Presentation.Api/Features/Dietologist/RecommendationTemplatesController.cs`

- `GET /api/v{version:apiVersion}/dietologist/recommendation-templates`
- `POST /api/v{version:apiVersion}/dietologist/recommendation-templates`
- `PUT /api/v{version:apiVersion}/dietologist/recommendation-templates/{templateId:guid}`
- `DELETE /api/v{version:apiVersion}/dietologist/recommendation-templates/{templateId:guid}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Dietologist/AttentionSignalTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/BulkCreateRecommendationsHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/ClientTaskDueReminderProcessorTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/ClientTaskHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistAccessPolicyTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.AcceptInvitationCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.DeclineInvitationCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.InviteCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.MappingTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.ReadQueryTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.RecommendationCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistFeatureTests.RelationshipCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistResidualCoverageTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/DietologistValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/RecommendationCommentHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Dietologist/RecommendationTemplateHandlerTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/DietologistInvitationInvariantTests.cs`
- `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/DietologistPersistenceIntegrationTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/DietologistEmailSenderTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DietologistClientsControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DietologistControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DietologistHttpMappingsTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DietologistInvitationsControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/DietologistNewEndpointsCoverageTests.cs`
- `tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationCurrentUserFlowTests.cs`
- `tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationNotificationIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
