# Hydration Domain

Keep `HydrationEntry` and `HydrationEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Reference central Domain one-way for `User`, `UserId`, and `DomainGuard` through explicit IVT. Preserve the forward EF navigation, validation, UTC normalization, and audit timestamp behavior. Do not add a reverse `User.HydrationEntries` navigation.
