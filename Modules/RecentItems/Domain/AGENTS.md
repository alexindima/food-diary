# RecentItems Domain

Own `RecentItem`, `RecentItemType` and `RecentItemId` with their legacy `FoodDiary.Domain` CLR namespaces. Reference Users Domain.Contracts for scalar UserId; retain no foreign User navigation in either direction.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
