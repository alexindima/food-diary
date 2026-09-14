# Billing Application Module Guidelines

## Scope

Rules for `Modules/Billing/Application/`.

## Responsibilities

- Own billing commands, queries, models, application services, and webhook orchestration.
- Register handlers, validators, and services through `AddBillingApplication`; Infrastructure exposes `AddBillingModule`.
- Depend on application-facing ports rather than infrastructure or provider implementations.

## Rules

- This is an extracted leaf module. The target dependency set is Application Abstractions, Domain, and the shared mediator.
- Do not reference presentation, infrastructure, integrations, or executable hosts.
- Provider idempotency and webhook ordering must remain explicit and covered by concurrency tests.
- Simple Billing mutations use the transactional command marker. Workflows with explicit transactions (renewal, queued webhook processing and inbox batches) use bare IRequest<T> so the mediator does not save again after failure bookkeeping. Keep each event commit and failure/backoff commit separate.

## Commands

- Build: `dotnet build Modules/Billing/Application/FoodDiary.Modules.Billing.Application.csproj`
- Tests: `dotnet test Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/FoodDiary.Modules.Billing.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

Transaction callbacks must reload previously captured subscription and inbox entities and user billing profiles on every attempt; keep provider calls outside replayable callbacks. See ADR 0032.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.

Use Users.Contracts IUserBillingService directly; do not recreate a forwarding user-context service or duplicate profile. Keep overview logic in GetBillingOverviewQueryHandler. Subscription eligibility is defined by BillingPremiumAccessPolicy.GrantsPremiumAccess: past_due and trialing require an end strictly after the current instant.

Renewal responses use a captured subscription snapshot and the same per-user transaction lock as webhooks. Record financial results even when the snapshot is stale, but never overwrite newer subscription/access state. Pending recurring payments are verified by payment ID after backoff; only a confirmed decline permits a new stable idempotency key. Transport failures reuse the current attempt key.

Reload and recheck provider, renewal status, cancellation and due time under the user lock for each selected batch item before calling the provider. Batch membership alone is not payment authorization. Provider HTTP stays outside replayable database callbacks. Apply provider occurrence timestamps to both subscription ordering and payment history; equal-time authoritative renewal updates may advance only the same payment. A verified declined renewal records a canceled payment but leaves the subscription past_due with retry backoff. Keep payment occurrence separate from subscription periods and preserve the same payment's period across HTTP and webhook arrival orders.

Checkout may reuse a customer identifier only for the same provider. Provider switches clear old subscription/method identifiers and ordering state; late notifications from the previous provider may update payment history but must not replace the current subscription.

Checkout owns its final transaction and uses bare IRequest: retain the checkout lease across provider HTTP and commit, but save under the same per-user transaction key as webhooks. Reload the user and subscription inside the callback and compare an immutable pre-provider version. A concurrent webhook must not be overwritten or followed by a duplicate subscription insert. Never replay provider HTTP in a database retry.

After the checkout cooldown, an idempotent provider may return an already saved session if HTTP response caching failed after commit. Under the user transaction lock, reuse only a pending checkout belonging to the same user and current subscription with matching provider, customer, price and plan. Do not insert it again or refresh the checkout timestamp; reject mismatched or finalized records.

Inbox batches dispatch individual queued-event requests. Exceptions roll back business writes before a separate transaction records a generic failure and backoff. Continue the batch after recorded failures; propagate cancellation and failure-bookkeeping errors.
