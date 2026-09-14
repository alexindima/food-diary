# Billing Infrastructure Guidelines

- Own Billing repositories, checkout lock, transaction runner and complete `AddBillingModule` registration.
- Own Billing provider adapters/options alongside repositories; depend on shared HTTP primitives where needed. Keep central database lifecycle/migrations in `FoodDiary.Infrastructure`.
- Translate unique-constraint races for provider event and external payment identifiers into existing idempotent semantics.

BillingDbContext owns subscriptions, payments and webhook records. Register it through the shared context factory. Repositories receive only owner sets and synchronize the current shared transaction before reads, including after intermediate saves and on scope reuse. The transaction runner retains owner advisory-lock SQL and delegates unconditional save, commit/retry and reset to IModuleTransactionCoordinator. Its exception translator runs after transaction disposal and before tracker cleanup, so Added entries remain available for exact duplicate classification. Duplicate payment/webhook translation inspects DbUpdateException.Entries so it works for owned tracking. Preserve exact PostgreSQL constraint names. Checkout session locks, provider adapters, User FKs and migrations remain unchanged.

Repository registrations use the live coordinator CurrentTransaction; neither registration nor the runner consumes FoodDiaryDbContext. PostgresBillingCheckoutLock retains its separate connection/lease behavior and the remaining central Infrastructure reference.

YooKassa recurring payments carry renewal=true and renewal_period_start metadata so verified webhooks and direct responses extend the same prior paid period. Keep the provider occurrence timestamp separate from that period anchor. Legacy renewals without the anchor are resolved from stored payment/subscription state in Application. Do not map a payment cancellation to a subscription cancellation for an identified renewal; expose the financial outcome and renewal marker to Application.
