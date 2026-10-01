# Cycles Infrastructure Guidelines

- Own the Cycles repository and complete module registration facade.
- CyclesDbContext owns the runtime profile and eight child entity mappings. Create it through the shared connection factory, inject only the CycleProfile DbSet into repositories and save via shared IUnitOfWork. Central Infrastructure must not reference this outer adapter.
- Keep migrations and the shared model snapshot central.

Preserve split queries, field-backed child navigations, user predicates and central migration/read bridges. User purge uses CyclesDbContext and synchronizes IModuleTransactionCoordinator.CurrentTransaction on every invocation. See ADR 0040.

Purge retains its existing order and user predicates, with no independent save or commit. Rebind the current transaction for scope reuse after commit/rollback; do not capture it when constructing the participant. The module has no direct or transitive central Infrastructure dependency. Users still owns final user deletion and its FK cascades.

Day notes are owned CycleDayNote children keyed by profile and calendar date. ClearDay and profile deletion remove them; notes do not create bleeding, symptom or fertility observations. General day text is exposed through the current-profile/day contracts and only included in the sensitive CSV export.
