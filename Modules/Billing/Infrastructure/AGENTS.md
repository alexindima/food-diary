# Billing Infrastructure Guidelines

- Own Billing repositories, checkout lock, transaction runner and complete `AddBillingModule` registration.
- Keep provider adapters in `FoodDiary.Integrations` and central database lifecycle/migrations in `FoodDiary.Infrastructure`.
- Translate unique-constraint races for provider event and external payment identifiers into existing idempotent semantics.
