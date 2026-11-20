# Images Infrastructure

ImageAssetCleanupBatch owns a fresh DI scope and save for one orphan candidate. Preserve caller-owned saving for ordinary deletion. Restrictive image FKs reject races atomically with outbox inserts; never weaken them to SET NULL. See ADR 0032.

Images PersistenceModel keeps owned mappings and UserId conversions through Users.Domain.Contracts. ImagesCrossModuleRelationships in central Infrastructure owns the unchanged User Cascade FK; the model must not reference foreign Domain assemblies.

ImageAssetContentService reads confirmed, owned assets by object key in the
published bucket or the server-only `private-confirmed/` staging prefix. Keep
reads bounded by the upload limit and cancellation deadline. Use data URLs
only for the immediate authorized response or provider request.

Private recipe confirmation sanitizes bytes under `private-confirmed/{raw key}`.
Presigned PUTs target only the separate raw key. Exclude the confirmed prefix
from staging rules that expire raw uploads. The API and JobManager storage
identity needs read, write and delete access to the confirmed prefix, and it
must stay outside CDN and public bucket rules. Promotion copies the confirmed
bytes to the stable public key and queues private copy deletion after the
asset transition saves. Account purge and orphan cleanup also queue all
potential objects for private assets.

Runtime writes and normal outbox processing use the owner context registered through
CreateModuleContext. Processors retain a shared-scope clean-entry callback before
claiming. Replay streams use their owner context on the shared connection. The central
coordinator resets all registered trackers and saves audit plus owner changes through IUnitOfWork. User purge retains its shared transaction scope. Image ownership reassignment uses
ImagesDbContext on the same connection and updates only requested IDs owned by the supplied source user.
It must not create, save or commit a transaction. Preserve caller rollback and reuse
after rollback, including resolving the service before the caller starts a transaction.

Resolve IModuleScopeGuard for the processor's clean-entry callback instead of the concrete shared context. Invoke the live check before each claim; the shared engine and owner claimer retain their existing lifecycle and local transaction checks.

ImagesUserDataPurgeParticipant uses ImagesDbContext with live coordinator transaction binding on every call. Order 135 follows Ai job removal (120) so restrictive image FKs remain valid. Enqueue deletion for staging and published variants as before; only Users saves and commits. The generic outbox engine and options belong to Shared/FoodDiary.Outbox.Infrastructure.

Shared outbox claiming, processing, policy, options and telemetry now belong to `Shared/FoodDiary.Outbox.Infrastructure` (see its AGENTS.md). Images, Notifications and Gamification Infrastructure reference that narrow runtime, never central Infrastructure, including transitively. Central Infrastructure retains replay coordination and the email adapter. The runtime checks `IModuleScopeGuard` on coordinated contexts; owner callbacks and dedicated-context clean-entry checks remain in force.

Use canonical FoodDiary.Modules.Images project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.

Confirmation and promotion hold the asset advisory lock and row lock through
commit. Deletion waits for any in-flight object writes. Pending assets are
reclaimed through the two-bucket cleanup when confirmation or promotion fails.
