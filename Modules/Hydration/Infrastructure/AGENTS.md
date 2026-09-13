# Hydration Infrastructure Guidelines

Hydration repository implementations and the complete `AddHydrationModule` composition facade live here. The facade creates a scoped HydrationDbContext through the shared provider/connection factory and injects its owned DbSets into repositories and interval reads. The runtime model contains only HydrationEntry and HydrationOperationReceipt. Do not inject the central context into those adapters. See ADR 0040.

The shared IUnitOfWork coordinates both trackers and one transaction; repositories must not save independently. Central Infrastructure must never reference this project. Migrations and composed reads remain central, as does the existing purge bridge during the pilot. PostgreSQL tests cover joint commits, retries, rollback, no-tracking reads, user cascade and unchanged constraints; see ADRs 0029 and 0040.

HydrationOperationReceiptRepository also receives only its owned DbSet. The create-from-operation command stages the entry and receipt together; the existing command unit of work commits them atomically. Permanent receipt keys survive entry deletion. Duplicate-key races roll back the losing entry and are retried by the durable bot; do not independently save either record. See ADR 0037.
