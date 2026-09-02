# Identity logical module

Identity owns authentication, account recovery, external login, token issuance,
login auditing, initial-admin bootstrap, and application email-template use cases.
Authentication and Email remain logical areas inside one application assembly.

Preserve the legacy `FoodDiary.Application.Identity` assembly and CLR namespaces.
Keep shared authentication/email contracts central. The User/Role CLR graph and credential state belong to Users Domain. Keep shared DbContext, migrations/snapshot, and combined UserRepository central as compatibility seams. Provider implementations, JWT/SSO/Redis adapters,
MailInbox/MailRelay integration, HTTP transport, and hosts remain with their current
owners.

Identity physically owns EmailTemplate, UserRefreshTokenSession, and UserLoginEvent
through its Domain project, plus their EF model and independent template/session
adapters. Central FoodDiaryDbContext applies the Identity persistence model; central
migrations/snapshot remain central. Identity Infrastructure also owns the login-event
reporting/cleanup repository and cached email-template provider. Their projections
may read Users data through the shared context; that read does not transfer User
ownership. Register all adapters with `AddIdentityPersistence`. See
`docs/ai/identity-domain-extraction.md`.
