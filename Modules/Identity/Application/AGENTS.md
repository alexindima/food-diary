# Identity Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Identity/`.

## Role

- Own authentication, account recovery, external login, token issuance, login auditing, initial-admin bootstrap, and application email-template use cases.
- Keep `Authentication` and `Email` as logical feature areas within one physical module.
- Depend on other business areas through their owner contracts, including a direct Notifications Application/Abstractions reference. Preserve semantic Users capabilities; do not acquire aggregate repository ports through an umbrella reference.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, identity services, and email administration services through `AddIdentityModule`.
- Keep HTTP authentication, provider implementations, persistence, transport, and host configuration outside this project.
