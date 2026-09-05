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
- Extracted project: `Modules/Billing/Application/FoodDiary.Application.Billing.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Billing/Application`
- `Modules/Billing/Application/Abstractions`
- `Modules/Billing/Infrastructure/Providers`
- `Modules/Billing/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: BillingSubscription, BillingPayment, BillingWebhookEvent
- Public contract files: 30
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 30
- Interfaces: 17
- DTO/read-model/projection types: 9
- Enums: 0
- Exported repository-shaped contracts: 10
- Contracts referencing domain entities: 5
- `class BillingErrors`
- `class BillingInputLimits`
- `class BillingPaymentAlreadyExistsException`
- `class BillingWebhookEventAlreadyProcessedException`
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
- `record BillingCheckoutSessionModel`
- `record BillingCheckoutSessionRequestModel`
- `record BillingPortalSessionModel`
- `record BillingPortalSessionRequestModel`
- `record BillingPublicConfigModel`
- `record BillingRecurringPaymentModel`
- `record BillingRecurringPaymentRequestModel`
- `record BillingSubscriptionOverviewReadModel`
- `record BillingWebhookEventModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.BaselineRegressionTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CheckoutCommandTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.OverviewAndContextTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.PortalAndTrialCommandTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.RenewalAndAccessServiceTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.WebhookCommandTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/NoopBillingCheckoutLock.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Domain.Tests/Domain/BillingInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Domain.Tests/Domain/BillingPaymentGuardTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/Integrations/BillingProviderGatewayAccessorTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/Integrations/BillingPublicConfigProviderTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/Services/BillingGatewayResilienceTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/Services/BillingGatewayTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/Services/PaddleNotificationRecoveryServiceTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/Services/YooKassaApiClientTests.cs`
- [presentation] `Modules/Billing/tests/FoodDiary.Modules.Billing.Presentation.Tests/BillingControllerTests.cs`
- [presentation] `Modules/Billing/tests/FoodDiary.Modules.Billing.Presentation.Tests/BillingHttpMappingsTests.cs`
- [presentation] `Modules/Billing/tests/FoodDiary.Modules.Billing.Presentation.Tests/BillingWebhookControllerTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/BillingModuleExtractionTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.JobManager.Tests/BillingRecoveryJobsTests.cs`
- [integration] `tests/FoodDiary.Web.Api.IntegrationTests/BillingSecurityIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
