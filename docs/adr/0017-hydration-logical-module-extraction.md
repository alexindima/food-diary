# ADR 0017: Hydration Logical Module Extraction

- Status: Accepted
- Date: 2026-08-29
- Owners: Backend architecture
- Related: ADR 0006, ADR 0016

> Updated 2026-09-01: the deferred domain-boundary phase is now complete.

## Context

The completed Fasting pilot proved an acyclic vertical module layout. Hydration is smaller, but its read service is consumed by Dashboard and Weekly Check-In and its EF relationship historically exposed a central `User.HydrationEntries` navigation.

## Decision

Move Hydration application implementation, repository ports, stable read contracts, repository implementation, and EF configuration under `Modules/Hydration`. Preserve the legacy application assembly name and existing command/model namespaces. Consumers outside composition roots reference Contracts; hosts reference Hydration Infrastructure and call `AddHydrationModule`.

Move `HydrationEntry` and `HydrationEntryId` to `Modules/Hydration/Domain` while preserving their CLR namespaces and behavior. The module references central Domain one-way for the compatibility `User` and `UserId` types. Production and test consumer review found no domain behavior using the inverse `User.HydrationEntries` collection, so remove that collection and configure the relationship from Hydration with `WithMany()`.

Keep `FoodDiaryDbContext`, migrations, and the model snapshot in central Infrastructure. Central Infrastructure references the Hydration Domain and persistence-model projects; Hydration Infrastructure references the central context, preventing a cycle. The table, schema, FK, cascade, check constraint, index and entity CLR identity remain unchanged; only the unused inverse CLR navigation is removed.

## Consequences

- HTTP routes, payloads, application namespaces, entity identity, tables, columns, indexes, and foreign keys remain stable.
- Dashboard and Weekly Check-In no longer reference the implementation assembly.
- The unused public `User.HydrationEntries` CLR member is removed; source/binary consumers must query through Hydration contracts or persistence ports.
- Domain ownership is physically complete while central database lifecycle remains explicit.
- Architecture tests and the project-reference matrix enforce physical ownership and the acyclic graph.
