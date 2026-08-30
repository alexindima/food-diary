# Daily Advices Persistence Model Guidelines

- Own the EF configuration and model-builder registration seam.
- Preserve table, columns, indexes, conversions, and CLR entity identity.
- Depend on module Domain and EF Core only; keep repositories and migrations outside.
