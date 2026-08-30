# Wearables Application Abstractions Guidelines

- Own provider client, repository, token-protection, OAuth-state, transaction, and read-model ports.
- Depend only on the Wearables domain, shared result primitives, and stable central identity types reached through the domain seam.
- Do not expose provider SDK DTOs, EF types, ASP.NET types, raw credentials, or host configuration.
