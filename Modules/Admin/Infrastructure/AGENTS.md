# Admin Infrastructure guidelines

Rules for `Modules/Admin/Infrastructure/`.

Own Admin billing reporting, impersonation session repository and protocol-specific handoff adapter. Preserve scoped aliases and singleton handoff. AddAdminModule composes application and persistence; AddAdminPersistence adds no handlers. Shared SSO store, JWT providers and central context remain outside this module.

AdminUserRoleAuditRepository is an Admin-owned read projection over Users-owned
role-audit entities. Register both scoped aliases here. Preserve its user filter,
left actor join, descending order, 1-50 limit and cancellation; do not move Users
entities/mappings or introduce central-to-module adapter references.
