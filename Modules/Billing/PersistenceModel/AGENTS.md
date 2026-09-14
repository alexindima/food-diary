# Billing Persistence Model Guidelines

- Own Billing EF configurations and explicit model-builder registration.
- Preserve table, column, key, index and relationship identity exactly.
- Keep the shared DbContext, historical migrations and snapshot central.

Use Users.Domain.Contracts for UserId. Central BillingCrossModuleRelationships
configures the User Cascade foreign keys of subscriptions and payments after owned
models. The payment-to-subscription SetNull relationship stays local. Do not add
Users.Domain back to PersistenceModel.
