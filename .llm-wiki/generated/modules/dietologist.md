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
- Abstraction-contract dependencies: Audit, Authentication, Email, Identity, Notifications, Users
- Business-module consumers: none observed
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

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ClientTask, DietologistInvitation, Recommendation, RecommendationBulkDispatch, RecommendationComment, RecommendationTemplate
- Public contract files: 33
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 33
- Interfaces: 22
- DTO/read-model/projection types: 9
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
- ... 3 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationCurrentUserFlowTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/DietologistInvitationNotificationIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/AttentionSignalTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Authentication/SecretInputLimitValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/BulkCreateRecommendationsHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/ClientTaskDueReminderProcessorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/ClientTaskHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistAccessPolicyTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.AcceptInvitationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.DeclineInvitationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.InviteCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.MappingTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.ProfileFixtures.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.ReadQueryTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.RecommendationCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.RelationshipCommandTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistInvitationTokenGeneratorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistProfileDisplayNameTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistResidualCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/DietologistValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/RecommendationCommentHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/RecommendationTemplateHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Validation/DietologistParserTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Validation/EnumValueParserTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/ClientTaskIdTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/ClientTaskInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/ClientTaskTimestampTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
