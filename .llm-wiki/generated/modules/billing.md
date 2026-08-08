---
id: generated.module.billing
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Billing

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Billing/FoodDiary.Application.Billing.csproj`
- Dependencies: none
- Consumers: none

## Source Areas

- `FoodDiary.Application.Abstractions/Billing`
- `FoodDiary.Domain/Entities/Billing`
- `FoodDiary.Infrastructure/Persistence/Billing`
- `FoodDiary.Infrastructure/Persistence/Configurations/Billing`
- `FoodDiary.Integrations/Billing`
- `FoodDiary.Presentation.Api/Features/Billing`
- `tests/FoodDiary.Application.Tests/Billing`

## HTTP Surface

### BillingController

Source: `FoodDiary.Presentation.Api/Features/Billing/BillingController.cs`

- `GET /api/v{version:apiVersion}/billing/overview`
- `POST /api/v{version:apiVersion}/billing/trial`
- `POST /api/v{version:apiVersion}/billing/checkout-session`
- `POST /api/v{version:apiVersion}/billing/portal-session`

### BillingWebhookController

Source: `FoodDiary.Presentation.Api/Features/Billing/BillingWebhookController.cs`

- `POST /api/v{version:apiVersion}/billing/webhooks/{provider}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.CheckoutCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.OverviewAndContextTests.cs`
- `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.PortalAndTrialCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.RenewalAndAccessServiceTests.cs`
- `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.WebhookCommandTests.cs`
- `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.cs`
- `tests/FoodDiary.ArchitectureTests/BillingModuleExtractionTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/BillingInvariantTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Integrations/BillingProviderGatewayAccessorTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Integrations/BillingPublicConfigProviderTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/BillingGatewayTests.cs`
- `tests/FoodDiary.JobManager.Tests/BillingRecoveryJobsTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/BillingControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/BillingHttpMappingsTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/BillingWebhookControllerTests.cs`
- `tests/FoodDiary.Web.Api.IntegrationTests/BillingSecurityIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
