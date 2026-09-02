# Hydration Domain

Keep `HydrationEntry` and `HydrationEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Reference Users Domain for `User`, Users Domain.Contracts for `UserId`, and shared Primitives for `DomainGuard`. Preserve the forward EF navigation, validation, UTC normalization, and audit timestamp behavior. Do not add a reverse `User.HydrationEntries` navigation.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
