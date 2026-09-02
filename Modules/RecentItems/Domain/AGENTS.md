# RecentItems Domain

Own `RecentItem`, `RecentItemType` and `RecentItemId` with their legacy `FoodDiary.Domain` CLR namespaces. The one-way `RecentItem.User` navigation may reference Users-owned `User`/`UserId`; never add an inverse `User.RecentItems` navigation.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
