# Billing Module Extraction: Wiki Findings

## Scope and authoritative inventory

The extraction verified current source, project references, EF mappings, focused tests and the Marketing extraction precedents (`bc73bbf2b0`, `2b11a94160`) before editing.

- Application owner: checkout, portal, trial, overview, webhook inbox/processing, renewal and entitlement services.
- Application ports/models: provider gateways, repositories, checkout lock, transaction runner and provider-facing models. `IBillingMarketingConversionRecorder` intentionally remains in central Application Abstractions because it is Billing's consumer-owned cross-module port implemented by Marketing.
- Domain owner: `BillingSubscription`, `BillingPayment`, `BillingWebhookEvent`, provider names and payment kinds.
- Persistence owner: three EF configurations, three repositories, `EfBillingTransactionRunner` and `PostgresBillingCheckoutLock`.
- Central seams: `FoodDiaryDbContext` Billing partial, migrations and snapshot; Users' `IUserBillingService`; Admin projections; Integrations provider adapters/options; Presentation HTTP transport; JobManager scheduling; Marketing conversion implementation.
- Focused tests: Billing application, domain and provider-boundary tests now live in three standalone projects under `Modules/Billing/tests`.

## Useful Wiki evidence

- `start` correctly classified the work as critical/governed and identified application, database and background-job acceptance obligations.
- `research` found the Users semantic capability, JobManager renewal consumer, current Billing ports and Marketing extraction Git precedents.
- `brief` with explicit planned paths correctly raised public API, persistence, security, privacy, topology and rollout risk.
- `test-plan` identified authentication scope, API compatibility, persistence concurrency, replay/idempotency, retry exhaustion and privacy lifecycle scenarios.
- `topology` found the Billing webhook controller/processor, inbox and renewal jobs, provider clients, retry/concurrency signals and the limitation that repository declarations cannot prove effective production exposure.
- `journeys` matched `FD-BILLING` with checkout, callback, activation, renewal, cancellation, failed-payment and idempotent-webhook scenarios.

## False positives and negatives

- The first `brief` call without explicit paths incorrectly selected ContentReports/Admin tests for a Billing extraction (false positive). Supplying verified planned paths corrected the classification.
- `ownership` returned empty direct/downstream module lists despite explicit Billing paths (false negative). The code/project graph showed Users, Marketing, Integrations, Presentation, JobManager, Initializer, Web API and Admin seams.
- `privacy` found customer/payment/price/email/token fields but omitted several domain/provider identifiers, webhook payload/signature boundaries, database uniqueness constraints and secret-bearing options (partial false negatives).
- `topology` enumerated many unrelated repository services and network policies because its output is repository-wide even with explicit Billing paths (noise/false positives for scoped review).
- `test-plan` without explicit paths returned zero files/commands/scenarios; explicit paths produced the expected plan.
- `decision` did not trigger ADR review even though project graph, DI and ownership changed. Current scoped instructions and precedents were sufficient, but this is a reproducible trigger gap.
- TypeScript prerequisites were unavailable, so commands used the read-only JSON baseline. The warning was useful, but it reduces confidence in freshness until `wiki update/verify` succeeds.

## Reproducible Wiki defects

These are general defects, not Billing-specific generator exceptions:

1. Intent-only module resolution can select an unrelated module; planned paths should be mandatory or heavily weighted for extraction tasks.
2. Scoped ownership can return an empty graph when a module is represented by a legacy extracted assembly plus central source areas.
3. Privacy indexing is primarily name-based and does not connect uniqueness constraints, webhook signature verification, transaction runners, retry policies or options-secret boundaries into one lifecycle view.
4. Topology scoping does not sufficiently filter repository-wide compose, hosted-service and network-policy inventory.
5. ADR/decision triggers miss physical module moves and DI/project-reference graph changes when runtime behavior is intentionally preserved.
6. The JSON-baseline fallback should report index age/source revision so callers can quantify staleness rather than only noting unavailable prerequisites.
