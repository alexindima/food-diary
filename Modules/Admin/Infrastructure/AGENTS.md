# Admin Infrastructure guidelines

Rules for `Modules/Admin/Infrastructure/`.

Own Admin billing reporting, impersonation session repository and protocol-specific handoff adapter. Preserve scoped aliases and singleton handoff. AddAdminModule composes application and persistence; AddAdminPersistence adds no handlers. Shared SSO store, JWT providers and central context remain outside this module.
