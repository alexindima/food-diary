# Wearables Application Abstractions Guidelines

- Own provider client, repository, token-protection, OAuth-state, transaction, and read-model ports.
- Depend only on the Wearables domain, shared result primitives, and stable central identity types reached through the domain seam.
- Do not expose provider SDK DTOs, EF types, ASP.NET types, raw credentials, or host configuration.

All module projects and tests use `FoodDiary.Modules.Wearables.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
