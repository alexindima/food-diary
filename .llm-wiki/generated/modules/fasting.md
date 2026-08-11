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

- Origin: module-graph
- Business-module dependencies: Notifications, Users
- Abstraction-contract dependencies: Notifications, Users
- Business-module consumers: Dashboard
- Host/adapter consumers: FoodDiary.JobManager, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Fasting`
- `FoodDiary.Application/Fasting`
- `FoodDiary.Domain/Entities/Tracking/Fasting`
- `FoodDiary.Infrastructure/Persistence/Configurations/Tracking`
- `FoodDiary.Infrastructure/Persistence/Tracking`
- `FoodDiary.Presentation.Api/Features/Fasting`

## HTTP Surface

### FastingController

Source: `FoodDiary.Presentation.Api/Features/Fasting/FastingController.cs`

- `POST /api/v{version:apiVersion}/fasting/start`
- `PUT /api/v{version:apiVersion}/fasting/end`
- `PUT /api/v{version:apiVersion}/fasting/current/duration`
- `PUT /api/v{version:apiVersion}/fasting/current/duration/reduce`
- `PUT /api/v{version:apiVersion}/fasting/current/check-in`
- `PUT /api/v{version:apiVersion}/fasting/current/skip-day`
- `PUT /api/v{version:apiVersion}/fasting/current/postpone-day`

### FastingInsightsController

Source: `FoodDiary.Presentation.Api/Features/Fasting/FastingInsightsController.cs`

- `GET /api/v{version:apiVersion}/fasting/stats`
- `GET /api/v{version:apiVersion}/fasting/insights`

### FastingReadController

Source: `FoodDiary.Presentation.Api/Features/Fasting/FastingReadController.cs`

- `GET /api/v{version:apiVersion}/fasting/current`
- `GET /api/v{version:apiVersion}/fasting/overview`
- `GET /api/v{version:apiVersion}/fasting/history`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: explicit-boundary-tests
- Declared owned entities: FastingPlan, FastingOccurrence, FastingCheckIn, FastingSession, FastingTelemetryEvent
- Public contract files: 22
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 22
- Interfaces: 17
- DTO/read-model/projection types: 3
- Enums: 0
- Exported repository-shaped contracts: 17
- Contracts referencing domain entities: 8
- `class FastingErrors`
- `interface IFastingCheckInReadModelRepository`
- `interface IFastingCheckInReadRepository`
- `interface IFastingCheckInRepository`
- `interface IFastingCheckInWriteRepository`
- `interface IFastingOccurrenceReadModelRepository`
- `interface IFastingOccurrenceReadRepository`
- `interface IFastingOccurrenceRepository`
- `interface IFastingOccurrenceWriteRepository`
- `interface IFastingPlanReadRepository`
- `interface IFastingPlanRepository`
- `interface IFastingPlanWriteRepository`
- `interface IFastingSessionReadRepository`
- `interface IFastingSessionRepository`
- `interface IFastingSessionWriteRepository`
- `interface IFastingTelemetryEventReadRepository`
- `interface IFastingTelemetryEventRepository`
- `interface IFastingTelemetryEventWriteRepository`
- `record FastingCheckInReadModel`
- `record FastingOccurrenceReadModel`
- `record FastingPlanReadModel`
- `record FastingTelemetryEventRecord`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Adjustments.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Cyclic.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Doubles.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.End.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Mappings.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Notifications.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Queries.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Start.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingInsightBuilderTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingNotificationPlannerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Fasting/FastingValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/FastingCheckInInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/FastingOccurrenceInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/FastingPlanInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/FastingSessionInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/FastingTelemetryEventTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.JobManager.Tests/FastingNotificationJobTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/FastingHttpMappingsTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/FastingTelemetrySummaryServiceTests.cs`
- [integration] `tests/FoodDiary.Web.Api.IntegrationTests/FastingApiIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
