# Billing Persistence Model Guidelines

- Own Billing EF configurations and explicit model-builder registration.
- Preserve table, column, key, index and relationship identity exactly.
- Keep the shared DbContext, historical migrations and snapshot central.
