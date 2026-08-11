---
id: generated.module.billing
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Billing

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Billing/FoodDiary.Application.Billing.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Billing`
- `FoodDiary.Application.Billing`
- `FoodDiary.Domain/Entities/Billing`
- `FoodDiary.Infrastructure/Persistence/Billing`
- `FoodDiary.Infrastructure/Persistence/Configurations/Billing`
- `FoodDiary.Integrations/Billing`
- `FoodDiary.Presentation.Api/Features/Billing`

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

## Boundary Health

- Role: aggregate-owner
- Physical isolation: assembly
- Architecture guardrails: assembly-isolated
- Declared owned entities: BillingSubscription, BillingPayment, BillingWebhookEvent
- Public contract files: 17
- Observed external consumer groups: 5
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 17
- Exported repository-shaped contracts: 10
- `interface IBillingCheckoutLock`
- `interface IBillingMarketingConversionRecorder`
- `interface IBillingPaymentReadRepository`
- `interface IBillingPaymentRepository`
- `interface IBillingPaymentWriteRepository`
- `interface IBillingProviderGateway`
- `interface IBillingProviderGatewayAccessor`
- `interface IBillingPublicConfigProvider`
- `interface IBillingRecurringProviderGateway`
- `interface IBillingSubscriptionReadModelRepository`
- `interface IBillingSubscriptionReadRepository`
- `interface IBillingSubscriptionRepository`
- `interface IBillingSubscriptionWriteRepository`
- `interface IBillingTransactionRunner`
- `interface IBillingWebhookEventReadRepository`
- `interface IBillingWebhookEventRepository`
- `interface IBillingWebhookEventWriteRepository`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.CheckoutCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.OverviewAndContextTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.PortalAndTrialCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.RenewalAndAccessServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.WebhookCommandTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/BillingModuleExtractionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/BillingInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Integrations/BillingProviderGatewayAccessorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Integrations/BillingPublicConfigProviderTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/BillingGatewayTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.JobManager.Tests/BillingRecoveryJobsTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/BillingControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/BillingHttpMappingsTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/BillingWebhookControllerTests.cs`
- [integration] `tests/FoodDiary.Web.Api.IntegrationTests/BillingSecurityIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
