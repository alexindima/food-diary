# RecentItems Domain

Own `RecentItem`, `RecentItemType` and `RecentItemId` with canonical folder namespaces. Reference Users Domain.Contracts for scalar UserId; retain no foreign User navigation in either direction.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.

Current module convention: all projects use `FoodDiary.Modules.RecentItems.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.

RecentItemReference pairs Product/Recipe kind with the appropriate owner scalar ID through private source-specific factories. Domain references only Products/Recipes Domain.Contracts for these identities. FromFields is the transport/storage adapter, including empty sentinels; CreateWithReference retains the aggregate's empty-ID and UTC/audit checks. Keep mapped polymorphic Guid columns and recency upsert/pruning semantics unchanged.
