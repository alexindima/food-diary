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
- `Modules/Fasting/Application.Abstractions`
- `Modules/Fasting/Contracts`
- `Modules/Fasting/Domain`
- `Modules/Fasting/Infrastructure`
- `Modules/Fasting/PersistenceModel`
- `Modules/Fasting/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: FastingPlan, FastingOccurrence, FastingCheckIn, FastingSession, FastingTelemetryEvent
- Public contract files: 14
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 14
- Interfaces: 0
- DTO/read-model/projection types: 8
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `record CleanupFastingTelemetryCommand`
- `record FastingCheckInModel`
- `record FastingInsightsModel`
- `record FastingMessageModel`
- `record FastingOverviewModel`
- `record FastingSessionModel`
- `record FastingStatsModel`
- `record FastingTelemetryPresetSummaryModel`
- `record FastingTelemetrySummaryModel`
- `record GetFastingTelemetrySummaryQuery`
- `record ReadCurrentFastingQuery`
- `record ReadFastingInsightsQuery`
- `record ReadFastingOverviewQuery`
- `record SendFastingNotificationsCommand`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Hosts/tests/FoodDiary.JobManager.Tests/FastingNotificationJobTests.cs`
- [behavioral-or-text-match] `Hosts/tests/FoodDiary.JobManager.Tests/FastingTelemetryCleanupJobTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/FastingApiIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingContractsGlobalUsings.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Adjustments.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Cyclic.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Doubles.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.End.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Mappings.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Notifications.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Queries.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.Start.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingInsightBuilderTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingNotificationPlannerTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingTelemetryCleanupServiceTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/FastingValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FastingCheckInInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FastingIdContractTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FastingOccurrenceInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FastingPlanInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FastingSessionInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Domain.Tests/FastingTelemetryEventTests.cs`
- [integration] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/FastingTelemetryEventRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/ModuleRegistrationTests.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/PostgresDatabaseCollection.cs`
- [behavioral-or-text-match] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Infrastructure.Tests/PostgresDatabaseFixture.cs`
- [presentation] `Modules/Fasting/tests/FoodDiary.Modules.Fasting.Presentation.Tests/ClientTelemetryLogHttpRequestValidationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
