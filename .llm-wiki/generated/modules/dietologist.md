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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Dietologist/Application`
- `Modules/Dietologist/Application/Abstractions`
- `Modules/Dietologist/Domain`
- `Modules/Dietologist/Infrastructure`
- `Modules/Dietologist/Infrastructure/Model`
- `Modules/Dietologist/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: ClientTask, DietologistInvitation, Recommendation, RecommendationBulkDispatch, RecommendationComment, RecommendationTemplate
- Public contract files: 35
- Observed external consumer groups: 3
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
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/Validation/DietologistParserTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/ClientTaskInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/DietologistIdContractTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/DietologistInvitationInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationBulkDispatchInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationCommentInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/Domain/RecommendationTemplateInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/Persistence/AttentionSignalMetricsReadServiceTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
