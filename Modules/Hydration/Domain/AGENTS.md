# Hydration Domain

Keep `HydrationEntry` and `HydrationEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Depend on Users Domain.Contracts for scalar `UserId` and shared Primitives for `DomainGuard`. Hydration Domain must not reference Users Domain or expose the User aggregate.

Preserve validation, UTC normalization, audit timestamps and the user-id invariant. PersistenceModel owns the navigation-free User FK and cascade mapping. Do not restore either `HydrationEntry.User` or `User.HydrationEntries`; see ADR 0029.
