# Admin Infrastructure guidelines

Rules for `Modules/Admin/Infrastructure/`.

Own the impersonation session repository, protocol-specific handoff adapter and the approved MailInbox client bridge. Keep the session writer scoped and the handoff singleton. AddAdminModule composes application and persistence; MailInbox integration remains an explicit host registration. Shared SSO store, JWT providers and central context remain outside this module.

AdminUserRoleAuditRepository and both scoped aliases live in FoodDiary.ReadModel.Composition, over Users-owned role-audit entities. Hosts register the composition project; do not move these aliases here. Preserve its user filter,
left actor join, descending order, 1-50 limit and cancellation; do not move Users
entities/mappings or introduce central-to-module adapter references.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.

Impersonation session writes accept only DbSet<AdminImpersonationSession>, supplied by AddAdminPersistence. The repository implements only IAdminImpersonationSessionWriteRepository. It has no reader dependency, cannot access other central tables and cannot save independently. Do not restore combined/read repository aliases. Existing composed reads remain behind IAdminImpersonationSessionQuery. AdminDbContext owns session and receipt tracking. AddAdminPersistence creates it through the shared context factory. Receipts retain immediate persistence through IUnitOfWork, duplicate detachment and existence verification. User purge remains on the central context inside the caller transaction; migrations and composed reads remain central.

Keep the impersonation repository directly in `Persistence`, with namespace `FoodDiary.Modules.Admin.Infrastructure.Persistence`.
