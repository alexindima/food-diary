# Cycles Infrastructure Guidelines

- Own the Cycles repository and complete module registration facade.
- CyclesDbContext owns the runtime profile and seven child entity mappings. Create it through the shared connection factory, inject only the CycleProfile DbSet into repositories and save via shared IUnitOfWork. Central Infrastructure must not reference this outer adapter.
- Keep migrations and the shared model snapshot central.

Preserve split queries, field-backed child navigations, user predicates and central migration/read/purge bridges. See ADR 0040.
