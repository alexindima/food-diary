# Hydration Domain

Keep `HydrationEntry` and `HydrationEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Depend on Users Domain.Contracts for scalar `UserId` and shared Primitives for `DomainGuard`. Hydration Domain must not reference Users Domain or expose the User aggregate.

Preserve validation, UTC normalization, audit timestamps and the user-id invariant. PersistenceModel owns the navigation-free User FK and cascade mapping. Do not restore either `HydrationEntry.User` or `User.HydrationEntries`; see ADR 0029.

Use canonical FoodDiary.Modules.Hydration project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.

`HydrationAmount` is the validated immutable milliliter quantity for one entry,
with the existing 1..10000 limit. Application handlers use `CreateWithAmount`
and `UpdateDetails`; primitive `Create`/`Update` remain compatibility adapters
with unchanged validation order. `AmountMl` stays an integer persistence/read
projection. Do not reuse entry limits for daily hydration targets, introduce
implicit numeric conversions, or change UTC normalization as part of typing.
