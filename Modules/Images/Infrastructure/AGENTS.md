# Images Infrastructure

ImageAssetCleanupBatch owns a fresh DI scope and save for one orphan candidate. Preserve caller-owned saving for ordinary deletion. Restrictive image FKs reject races atomically with outbox inserts; never weaken them to SET NULL. See ADR 0032.

Images PersistenceModel keeps owned mappings and UserId conversions through Users.Domain.Contracts. ImagesCrossModuleRelationships in central Infrastructure owns the unchanged User Cascade FK; the model must not reference foreign Domain assemblies.

ImageAssetContentService reads confirmed, owned assets by ObjectKey from the
configured published bucket. It never fetches the stored public URL. Keep reads
bounded by the upload limit and cancellation deadline, and expose only safe
storage errors. Data URLs are transient provider input, not persisted state.

Runtime writes and normal outbox processing use the owner context registered through
CreateModuleContext. Processors retain a shared-scope clean-entry callback before
claiming. Replay streams keep the central context for the existing audit/reset
transaction. User purge retains its shared transaction scope. Image ownership reassignment uses
ImagesDbContext on the same connection and executes only the requested-ID bulk update.
It must not create, save or commit a transaction. Preserve caller rollback and reuse
after rollback, including resolving the service before the caller starts a transaction.
