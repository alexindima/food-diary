# Admin Infrastructure/Model guidelines

Rules for `Modules/Admin/Infrastructure/Model/`.

Own AdminImpersonationSession EF configuration and ApplyAdminPersistenceModel. Shared context applies it explicitly. Keep existing indexes, FK/delete rules and lengths; migrations and snapshot remain central.

Admin also owns BugAcknowledgementReceipt and its mapping: only the opaque MailInbox stored ID is retained for receipt deduplication. It has no user/content foreign key.

Use Users.Domain.Contracts for scalar UserId. AdminCrossModuleRelationships in
central Infrastructure composes foreign User relationships after owned models.
ActorUserId and TargetUserId retain Restrict deletion. Do not restore Users.Domain to PersistenceModel.
