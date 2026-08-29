# ADR 0017: Hydration Logical Module Extraction

- Status: Accepted
- Date: 2026-08-29
- Owners: Backend architecture
- Related: ADR 0006, ADR 0016

## Context

The completed Fasting pilot proved an acyclic vertical module layout. Hydration is smaller, but its read service is consumed by Dashboard and Weekly Check-In and its EF relationship historically exposed a central `User.HydrationEntries` navigation.

## Decision

Move Hydration application implementation, repository ports, stable read contracts, repository implementation, and EF configuration under `Modules/Hydration`. Preserve the legacy application assembly name and existing command/model namespaces. Consumers outside composition roots reference Contracts; hosts reference Hydration Infrastructure and call `AddHydrationModule`.

Keep `HydrationEntry`, `HydrationEntryId`, and the public `User.HydrationEntries` navigation in central Domain as a deliberate compatibility seam. Moving those types would require central Domain to reference a child module or would remove an existing CLR and EF aggregate navigation. That boundary change is deferred until it can be designed and versioned independently.

Keep `FoodDiaryDbContext`, migrations, and the model snapshot in central Infrastructure. Central Infrastructure references the Hydration persistence-model project; Hydration Infrastructure references the central context, preventing a cycle. The database relationship and two-way CLR navigation remain unchanged.

## Consequences

- HTTP routes, payloads, application namespaces, entity identity, tables, columns, indexes, and foreign keys remain stable.
- Dashboard and Weekly Check-In no longer reference the implementation assembly.
- Existing CLR consumers of `User.HydrationEntries` remain compatible.
- Domain ownership is intentionally incomplete in this increment and is documented rather than hidden behind a breaking change.
- Architecture tests and the project-reference matrix enforce physical ownership and the acyclic graph.
