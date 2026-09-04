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
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Audit, Authentication, Email, Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Dietologist`
- `Modules/Dietologist/Application`
- `Modules/Dietologist/Application/Abstractions`
- `Modules/Dietologist/Domain`
- `Modules/Dietologist/Infrastructure`
- `Modules/Dietologist/Infrastructure/Model`

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
- Declared owned entities: ClientTask, DietologistInvitation, Recommendation, RecommendationBulkDispatch, RecommendationComment, RecommendationTemplate
- Public contract files: 0
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Interfaces: 0
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/AttentionSignalTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/BulkCreateRecommendationsHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/ClientTaskDueReminderProcessorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/ClientTaskHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistAccessPolicyTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.AcceptInvitationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.DeclineInvitationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.InviteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.RecommendationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.RelationshipCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistInvitationTokenGeneratorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistResidualCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/DietologistValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/RecommendationCommentHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Dietologist/RecommendationTemplateHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/ClientTaskInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/DietologistIdContractTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/DietologistInvitationInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationBulkDispatchInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationCommentInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationTemplateInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/Persistence/AttentionSignalMetricsReadServiceTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/Persistence/CollaborationAuditInterceptorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/Persistence/CollaborationAuditRegistrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
