---
id: generated.module.fasting
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Fasting

## Graph

- Origin: module-graph
- Dependencies: Notifications, Users
- Consumers: Dashboard

## Source Areas

- `FoodDiary.Application.Abstractions/Fasting`
- `FoodDiary.Application/Fasting`
- `FoodDiary.Domain/Entities/Tracking/Fasting`
- `FoodDiary.Presentation.Api/Features/Fasting`
- `FoodDiary.Web.Client/src/app/features/fasting`
- `tests/FoodDiary.Application.Tests/Fasting`

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

## Focused Tests

- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Adjustments.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Cyclic.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Doubles.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.End.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Mappings.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Notifications.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Queries.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.Start.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingInsightBuilderTests.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingNotificationPlannerTests.cs`
- `tests/FoodDiary.Application.Tests/Fasting/FastingValidatorTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/FastingCheckInInvariantTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/FastingOccurrenceInvariantTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/FastingPlanInvariantTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/FastingSessionInvariantTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/FastingTelemetryEventTests.cs`
- `tests/FoodDiary.JobManager.Tests/FastingNotificationJobTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/FastingHttpMappingsTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/FastingTelemetrySummaryServiceTests.cs`
- `tests/FoodDiary.Web.Api.IntegrationTests/FastingApiIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
