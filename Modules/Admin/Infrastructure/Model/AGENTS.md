# Admin Infrastructure/Model guidelines

Rules for `Modules/Admin/Infrastructure/Model/`.

Own AdminImpersonationSession EF configuration and ApplyAdminPersistenceModel. Shared context applies it explicitly. Keep existing indexes, FK/delete rules and lengths; migrations and snapshot remain central.
