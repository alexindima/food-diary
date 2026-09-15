# Identity Application Module Guidelines

## Scope

Rules for `Modules/Identity/Application/`.

## Role

- Own authentication, account recovery, external login, token issuance, login auditing, initial-admin bootstrap, and application email-template use cases.
- Keep `Authentication` and `Email` as logical feature areas within one physical module.
- Depend on other business areas through their owner contracts, including a direct Notifications Application.Abstractions reference. Preserve semantic Users capabilities; do not acquire aggregate repository ports through an umbrella reference.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, identity services, and email administration services through `AddIdentityModule`.
- Keep HTTP authentication, provider implementations, persistence, transport, and host configuration outside this project.

Consume RoleNames through Users Domain.Contracts; do not reference Users Domain for role constants. User mutation remains behind Users capabilities.

Login-event owner query handlers consume IUserLoginEventQuery directly. This existing composed projection port preserves paging/filtering and device summaries without acquiring the repository's write/retention implementation. Hosts continue registering AddReadModelComposition.

Use canonical FoodDiary.Modules.Identity project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.

StartTelegramBackupEmail and CompleteTelegramBackupEmail own their OIDC use-case logic. The internal TelegramBackupEmailOidcAttempt record preserves the shared ticket payload, purpose and browser/account binding; do not restore a forwarding service for these single-caller operations.
