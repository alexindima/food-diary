# Hydration Infrastructure Guidelines

Hydration repository implementations and the complete `AddHydrationModule` composition facade live here. The facade creates a scoped HydrationDbContext through the shared provider/connection factory and injects its owned DbSets into repositories and interval reads. The runtime model contains only HydrationEntry and HydrationOperationReceipt. Do not inject the central context into those adapters. See ADR 0040.

The shared IUnitOfWork coordinates both trackers and one transaction; repositories must not save independently. Central Infrastructure must never reference this project. Migrations and composed reads remain central. User purge uses HydrationDbContext and synchronizes IModuleTransactionCoordinator.CurrentTransaction on every invocation. PostgreSQL tests cover joint commits, retries, rollback, no-tracking reads, user cascade and unchanged constraints; see ADRs 0029 and 0040.

HydrationOperationReceiptRepository also receives only its owned DbSet. The create-from-operation command uses IHydrationOperationTransactionRunner to lock the user/operation pair before reading its receipt and atomically commit the entry and receipt. Permanent receipt keys survive entry deletion. Concurrent retries wait for the owner lock and return the existing result or OperationConflict for different input. Do not independently save either record. See ADR 0037.

Hydration registration consumes IModuleContextFactory from FoodDiary.Persistence.Abstractions. Do not restore concrete FoodDiaryDbContext access in ModuleRegistration; the purge participant receives only its owner context and the narrow live transaction coordinator.

Purge retains its existing order and user predicates, with no independent save or commit. Rebind the current transaction for scope reuse after commit/rollback; do not capture it when constructing the participant. The module has no direct or transitive central Infrastructure dependency. Users still owns final user deletion and its FK cascades.

Use canonical FoodDiary.Modules.Hydration project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
