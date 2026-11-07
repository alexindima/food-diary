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
- Extracted project: `Modules/Billing/Application/FoodDiary.Modules.Billing.Application.csproj`
- Business-module dependencies: Identity, Marketing, Users
- Abstraction-contract dependencies: Identity, Marketing, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Billing/Application`
- `Modules/Billing/Application.Abstractions`
- `Modules/Billing/Infrastructure/Providers`
- `Modules/Billing/Presentation`

## HTTP Surface

### BillingController

Source: `Modules/Billing/Presentation/Controllers/BillingController.cs`

- `GET /api/v{version:apiVersion}/billing/overview`
- `POST /api/v{version:apiVersion}/billing/trial`
- `POST /api/v{version:apiVersion}/billing/checkout-session`
- `POST /api/v{version:apiVersion}/billing/portal-session`
- `POST /api/v{version:apiVersion}/billing/cancel`
- `POST /api/v{version:apiVersion}/billing/cancellations/{cancellationId:guid}/feedback`

### BillingWebhookController

Source: `Modules/Billing/Presentation/Controllers/BillingWebhookController.cs`

- `POST /api/v{version:apiVersion}/billing/webhooks/{provider}`

### TelegramStarsController

Source: `Modules/Billing/Presentation/Controllers/TelegramStarsController.cs`

- `POST /api/v{version:apiVersion}/billing/telegram-stars/updates`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: BillingSubscription, BillingPayment, BillingWebhookEvent, BillingCancellation
- Public contract files: 33
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 33
- Interfaces: 15
- DTO/read-model/projection types: 14
- Enums: 0
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 0
- `class BillingErrors`
- `class BillingInputLimits`
- `class BillingPaymentAlreadyExistsException`
- `class BillingWebhookEventAlreadyProcessedException`
- `interface IBillingCancellationRepository`
- `interface IBillingCheckoutLock`
- `interface IBillingPaymentReadRepository`
- `interface IBillingPaymentWriteRepository`
- `interface IBillingProviderGateway`
- `interface IBillingProviderGatewayAccessor`
- `interface IBillingPublicConfigProvider`
- `interface IBillingRecurringProviderGateway`
- `interface IBillingSubscriptionReadModelRepository`
- `interface IBillingSubscriptionWriteRepository`
- `interface IBillingTransactionRunner`
- `interface IBillingWebhookEventReadRepository`
- `interface IBillingWebhookEventWriteRepository`
- `interface IPaddleNotificationRecoveryGateway`
- `interface ITelegramStarsBotAuthenticator`
- `record BillingCancellationEmailCandidateModel`
- `record BillingCancellationRequestModel`
- `record BillingCancellationSnapshotModel`
- `record BillingCheckoutSessionModel`
- `record BillingCheckoutSessionRequestModel`
- `record BillingPortalSessionModel`
- `record BillingPortalSessionRequestModel`
- `record BillingPublicConfigModel`
- `record BillingRecurringPaymentModel`
- `record BillingRecurringPaymentRequestModel`
- `record BillingSubscriptionOverviewReadModel`
- ... 3 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Hosts/tests/FoodDiary.JobManager.Tests/BillingCancellationEmailJobTests.cs`
- [behavioral-or-text-match] `Hosts/tests/FoodDiary.JobManager.Tests/BillingRecoveryJobsTests.cs`
- [behavioral-or-text-match] `Hosts/tests/FoodDiary.Telegram.Bot.Tests/BillingOverviewContractTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/BillingCancellationTransportTests.cs`
- [integration] `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/BillingSecurityIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.AccessRevocationSecurityTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.BaselineRegressionTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CancellationBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CancellationEmailBoundaries.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CancellationEmailTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CancellationTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CancellationWebhookOrdering.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CheckoutCommandTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.CheckoutWebhookConsistencyTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.InboxResilienceTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.OrderingRegressionTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.OverviewAndContextTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.PortalAndTrialCommandTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.RenewalAndAccessServiceTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.RenewalConsistencyTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.RenewalMetadataTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.RenewalSchedulingTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.TelegramStarsBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.TelegramStarsTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.TrialIdentitySecurityTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.WebhookCommandTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/BillingFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/NoopBillingCheckoutLock.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/Billing/WebhookDeduplicationMetadataTests.cs`
- [behavioral-or-text-match] `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/BillingPaymentAlreadyExistsExceptionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
