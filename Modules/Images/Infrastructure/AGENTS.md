# Images Infrastructure

ImageAssetCleanupBatch owns a fresh DI scope and save for one orphan candidate. Preserve caller-owned saving for ordinary deletion. Restrictive image FKs reject races atomically with outbox inserts; never weaken them to SET NULL. See ADR 0032.
