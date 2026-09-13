# Billing Infrastructure Guidelines

- Own Billing repositories, checkout lock, transaction runner and complete `AddBillingModule` registration.
- Own Billing provider adapters/options alongside repositories; depend on shared HTTP primitives where needed. Keep central database lifecycle/migrations in `FoodDiary.Infrastructure`.
- Translate unique-constraint races for provider event and external payment identifiers into existing idempotent semantics.

BillingDbContext owns subscriptions, payments and webhook records. Register it through the shared context factory. Repositories receive only owner sets and synchronize the current shared transaction before reads, including after intermediate saves and on scope reuse. The transaction runner retains advisory locks and commit/retry coordination, but saves through IUnitOfWork. Duplicate payment/webhook translation inspects DbUpdateException.Entries so it works for owned tracking. Preserve exact PostgreSQL constraint names. Checkout session locks, provider adapters, User FKs and migrations remain unchanged.
