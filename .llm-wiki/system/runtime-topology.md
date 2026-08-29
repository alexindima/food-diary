---
id: system.runtime-topology
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiRuntimeTopology.ps1
sources:
  - .llm-wiki/generated/runtime-topology.json
  - docker-compose.yml
  - FoodDiary.Presentation.Api/Features/Billing/BillingWebhookController.cs
  - FoodDiary.Application.Billing/Services/BillingWebhookInboxService.cs
  - FoodDiary.Application.Billing/Commands/ProcessBillingWebhook/BillingWebhookEventProcessor.cs
  - FoodDiary.Infrastructure/Persistence/Billing/EfBillingTransactionRunner.cs
  - FoodDiary.JobManager/Services/RecurringJobsHostedService.cs
  - tests/FoodDiary.Infrastructure.IntegrationTests/Integration/PersistenceRepositoryCoverageIntegrationTests.cs
---

# Runtime Topology

The generated topology inventories Docker Compose services, hosted workers,
typed or direct `HttpClient` consumers, webhook-related types, and recurring job
registrations. Compose records include declared ports, profiles, networks,
environment-key names, mounts, dependencies, and selected container-hardening
flags. Webhook and outbound-network records include inferred security signals.

Every record distinguishes repository declarations or code inference from
runtime proof. The topology cannot establish effective production exposure,
cloud IAM or database grants, DNS answers at connect time, proxy/redirect
behavior, certificate validation, or webhook replay/idempotency. Deployed
topology, provider dashboards, runtime probes, and environment-specific
infrastructure remain authoritative.

## Billing webhook replay path

For billing replay or idempotency work, trace the complete path instead of
stopping at the HTTP handler:

`BillingWebhookController` -> `ProcessBillingWebhookCommandHandler` -> queued
`BillingWebhookEvent` -> `RecurringJobIds.BillingWebhookInbox` ->
`BillingWebhookInboxService` -> `BillingWebhookEventProcessor` ->
`EfBillingTransactionRunner` -> PostgreSQL unique constraints for provider event
and external payment identifiers.

Check both the application webhook/concurrency tests and the provider-backed
transaction-runner tests. A recognized `DbUpdateException` rolls back the
database transaction but does not reset EF tracking state by itself; verify that
the runner clears rejected tracked mutations before any follow-up transaction
reattaches and persists the inbox completion.
