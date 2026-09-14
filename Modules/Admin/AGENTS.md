# Admin logical module

Rules for `Modules/Admin/`.


## Admin physical ownership

All seven production projects are sibling directories: Application, Application.Abstractions, Contracts, Domain, Infrastructure, PersistenceModel and Presentation. Do not nest their project files. PersistenceModel retains its EF mappings; architecture source discovery includes this sibling directory.

Admin owns application slices, billing-report/impersonation/mail-reader ports,
AdminImpersonationSession Domain, its explicit EF model and reporting/session
adapters under Modules/Admin. The legacy application assembly name remains stable; compatibility requires coordinated host rebuilds. Email templates
remain Identity-owned and role audit/User capabilities remain Users-owned despite
legacy Admin namespaces. Shared context/migrations/cleanup, SSO store/JWT providers,
HTTP authorization, structured audit and MailInbox client bridge remain central.
Hosts call AddAdminModule; JobManager adds only AddAdminPersistence. See
docs/ai/admin-ownership-inventory.md for current source evidence and test ownership.

Do not acquire foreign write repositories or move authentication transport/provider logic. Keep SQL, authorization, impersonation expiry/audit, cancellation and HTTP contracts unchanged. No empty Contracts layer is needed.

The role-audit read projection and its DI registration belong to FoodDiary.ReadModel.Composition. Its focused Admin tests stay under Modules/Admin/tests. UserRoleAuditEvent, role membership and their mappings belong to Users. See `docs/adr/0038-read-model-composition.md`.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

Identity Presentation consumes only ExchangeAdminImpersonationCommand from Admin Contracts; the handler and protocol remain Admin-owned.

Do not repeat an `Admin` grouping directory inside Application, Application.Abstractions, Contracts, Domain, Infrastructure, PersistenceModel or Presentation. Do not override RootNamespace: namespaces follow the .csproj filename and physical folders. AdminNamespaceTests checks all production and test projects, and IDE0130 enforces this during compilation.

Presentation uses sibling Controllers, Requests, Responses, Mappings and Extensions folders. Do not reintroduce a Features wrapper for the whole module.
