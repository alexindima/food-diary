# Images Infrastructure

ImageAssetCleanupBatch owns a fresh DI scope and save for one orphan candidate. Preserve caller-owned saving for ordinary deletion. Restrictive image FKs reject races atomically with outbox inserts; never weaken them to SET NULL. See ADR 0032.

Images PersistenceModel keeps owned mappings and UserId conversions through Users.Domain.Contracts. ImagesCrossModuleRelationships in central Infrastructure owns the unchanged User Cascade FK; the model must not reference foreign Domain assemblies.

ImageAssetContentService reads confirmed, owned assets by ObjectKey from the
configured published bucket. It never fetches the stored public URL. Keep reads
bounded by the upload limit and cancellation deadline, and expose only safe
storage errors. Data URLs are transient provider input, not persisted state.

Runtime writes and normal outbox processing use the owner context registered through
CreateModuleContext. Processors retain a shared-scope clean-entry callback before
claiming. Replay streams use their owner context on the shared connection. The central
coordinator resets all registered trackers and saves audit plus owner changes through IUnitOfWork. User purge retains its shared transaction scope. Image ownership reassignment uses
ImagesDbContext on the same connection and executes only the requested-ID bulk update.
It must not create, save or commit a transaction. Preserve caller rollback and reuse
after rollback, including resolving the service before the caller starts a transaction.

Resolve IModuleScopeGuard for the processor's clean-entry callback instead of the concrete shared context. Invoke the live check before each claim; the shared engine and owner claimer retain their existing lifecycle and local transaction checks.

ImagesUserDataPurgeParticipant uses ImagesDbContext with live coordinator transaction binding on every call. Order 135 follows Ai job removal (120) so restrictive image FKs remain valid. Enqueue deletion for staging and published variants as before; only Users saves and commits. The generic outbox engine and options belong to Shared/FoodDiary.Outbox.Infrastructure.

Shared outbox claiming, processing, policy, options and telemetry now belong to `Shared/FoodDiary.Outbox.Infrastructure` (see its AGENTS.md). Images, Notifications and Gamification Infrastructure reference that narrow runtime, never central Infrastructure, including transitively. Central Infrastructure retains replay coordination and the email adapter. The runtime checks `IModuleScopeGuard` on coordinated contexts; owner callbacks and dedicated-context clean-entry checks remain in force.
