# RecentItems Domain

Own `RecentItem`, `RecentItemType` and `RecentItemId` with their legacy `FoodDiary.Domain` CLR namespaces. The one-way `RecentItem.User` navigation may reference Users-owned `User`/`UserId`; never add an inverse `User.RecentItems` navigation.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
