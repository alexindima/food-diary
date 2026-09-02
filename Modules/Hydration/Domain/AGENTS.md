# Hydration Domain

Keep `HydrationEntry` and `HydrationEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Reference Users Domain for `User`, Users Domain.Contracts for `UserId`, and central Domain for `DomainGuard` through explicit IVT. Preserve the forward EF navigation, validation, UTC normalization, and audit timestamp behavior. Do not add a reverse `User.HydrationEntries` navigation.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
