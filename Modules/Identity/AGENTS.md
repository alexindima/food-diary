# Identity logical module

Identity owns authentication, account recovery, external login, token issuance,
login auditing, initial-admin bootstrap, and application email-template use cases.
Authentication and Email remain logical areas inside one application assembly.

Preserve the legacy `FoodDiary.Application.Identity` assembly and CLR namespaces.
Keep shared authentication/email contracts central. Keep the User/Role CLR graph,
credential state, shared DbContext, migrations/snapshot, and combined UserRepository
central as compatibility seams. Provider implementations, JWT/SSO/Redis adapters,
MailInbox/MailRelay integration, HTTP transport, and hosts remain with their current
owners.
