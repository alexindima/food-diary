# Admin Infrastructure guidelines

Rules for `Modules/Admin/Infrastructure/`.

Own Admin billing reporting, impersonation session repository, protocol-specific handoff adapter and the approved MailInbox client bridge. Preserve scoped aliases and singleton handoff. AddAdminModule composes application and persistence; MailInbox integration remains an explicit host registration. Shared SSO store, JWT providers and central context remain outside this module.

AdminUserRoleAuditRepository is an Admin-owned read projection over Users-owned
role-audit entities. Register both scoped aliases here. Preserve its user filter,
left actor join, descending order, 1-50 limit and cancellation; do not move Users
entities/mappings or introduce central-to-module adapter references.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.
