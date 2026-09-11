# Images Infrastructure

ImageAssetCleanupBatch owns a fresh DI scope and save for one orphan candidate. Preserve caller-owned saving for ordinary deletion. Restrictive image FKs reject races atomically with outbox inserts; never weaken them to SET NULL. See ADR 0032.

Images PersistenceModel keeps owned mappings and UserId conversions through Users.Domain.Contracts. ImagesCrossModuleRelationships in central Infrastructure owns the unchanged User Cascade FK; the model must not reference foreign Domain assemblies.

ImageAssetContentService reads confirmed, owned assets by ObjectKey from the
configured published bucket. It never fetches the stored public URL. Keep reads
bounded by the upload limit and cancellation deadline, and expose only safe
storage errors. Data URLs are transient provider input, not persisted state.
