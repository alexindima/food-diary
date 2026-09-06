---
id: generated.module.fasting
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Fasting

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Fasting/Application/FoodDiary.Modules.Fasting.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Fasting/Application`
- `Modules/Fasting/Application/Abstractions`
- `Modules/Fasting/Contracts`
- `Modules/Fasting/Domain`
- `Modules/Fasting/Infrastructure`
- `Modules/Fasting/Infrastructure/Model`
- `Modules/Fasting/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: FastingPlan, FastingOccurrence, FastingCheckIn, FastingSession, FastingTelemetryEvent
- Public contract files: 36
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 36
- Interfaces: 21
- DTO/read-model/projection types: 12
- Enums: 0
- Exported repository-shaped contracts: 17
- Contracts referencing domain entities: 9
- `class FastingErrors`
- `interface IFastingCheckInReadModelRepository`
- `interface IFastingCheckInReadRepository`
- `interface IFastingCheckInRepository`
- `interface IFastingCheckInWriteRepository`
- `interface IFastingNotificationScheduler`
- `interface IFastingOccurrenceReadModelRepository`
- `interface IFastingOccurrenceReadRepository`
- `interface IFastingOccurrenceRepository`
- `interface IFastingOccurrenceWriteRepository`
- `interface IFastingPlanReadRepository`
- `interface IFastingPlanRepository`
- `interface IFastingPlanWriteRepository`
- `interface IFastingReadService`
- `interface IFastingSessionReadRepository`
- `interface IFastingSessionRepository`
- `interface IFastingSessionWriteRepository`
- `interface IFastingTelemetryCleanupService`
- `interface IFastingTelemetryEventReadRepository`
- `interface IFastingTelemetryEventRepository`
- `interface IFastingTelemetryEventWriteRepository`
- `interface IFastingTelemetrySummaryReadService`
- `record FastingActiveOccurrenceModel`
- `record FastingCheckInModel`
- `record FastingCheckInReadModel`
- `record FastingInsightsModel`
- `record FastingMessageModel`
- `record FastingOccurrenceReadModel`
- `record FastingOverviewModel`
- `record FastingPlanReadModel`
- ... 6 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Adjustments.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Cyclic.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Doubles.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.End.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Mappings.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Notifications.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Queries.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.Start.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingInsightBuilderTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingNotificationPlannerTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingTelemetryCleanupServiceTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Fasting/FastingValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingContractsGlobalUsings.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/Domain/FastingCheckInInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/Domain/FastingIdContractTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/Domain/FastingOccurrenceInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/Domain/FastingPlanInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/Domain/FastingSessionInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/Domain/FastingTelemetryEventTests.cs`
- [integration] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/FastingTelemetryEventRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/PostgresDatabaseCollection.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/PostgresDatabaseFixture.cs`
- [presentation] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Presentation.Tests/ClientTelemetryLogHttpRequestValidationTests.cs`
- [presentation] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Presentation.Tests/FastingHttpMappingsTests.cs`
- [presentation] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Presentation.Tests/FastingReadControllerTests.cs`
- [presentation] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Presentation.Tests/FastingTelemetrySummaryServiceTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
