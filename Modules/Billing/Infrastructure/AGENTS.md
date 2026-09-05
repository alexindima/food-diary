# Billing Infrastructure Guidelines

- Own Billing repositories, checkout lock, transaction runner and complete `AddBillingModule` registration.
- Own Billing provider adapters/options alongside repositories; depend on shared HTTP primitives where needed. Keep central database lifecycle/migrations in `FoodDiary.Infrastructure`.
- Translate unique-constraint races for provider event and external payment identifiers into existing idempotent semantics.
