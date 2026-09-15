# BodyMetrics Infrastructure Guidelines

## Scope

Rules for `Modules/BodyMetrics/Infrastructure/`.

## Boundaries

- Own BodyMetrics repository implementations and complete module registration.
- Preserve all user predicates, ordering, tracking, date, and cancellation behavior.
- Keep central `FoodDiaryDbContext`, migrations, and snapshot outside this module.
- BodyMetricsDbContext owns runtime WeightEntry/WaistEntry tracking. Registration uses the central CreateModuleContext factory and supplies only owned DbSets to repositories. Save through the shared IUnitOfWork; never commit in repositories. Retain the central migration/read model and Users-owned goals. User purge uses BodyMetricsDbContext and synchronizes IModuleTransactionCoordinator.CurrentTransaction on every invocation. See ADR 0040.

Purge retains its existing order and user predicates, with no independent save or commit. Rebind the current transaction for scope reuse after commit/rollback; do not capture it when constructing the participant. The module has no direct or transitive central Infrastructure dependency. Users still owns final user deletion and its FK cascades.

BodyMetricsDbContext translates only public WeightEntries/IX_WeightEntries_UserId_Date and WaistEntries/IX_WaistEntries_UserId_Date unique violations into DbUpdateConcurrencyException. The shared unit of work owns rollback; HTTP uses the existing Concurrency.Conflict 409 response. Other provider errors must propagate unchanged. Sequential duplicate behavior remains unchanged.
