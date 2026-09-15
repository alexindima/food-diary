# RecentItems Application Abstractions

Own only the repository composition interfaces. Semantic usage read/recording
interfaces and immutable records belong to RecentItems Contracts. Depend directly
on that narrow project; use canonical namespaces and preserve post-commit behavior.

Current module convention: all projects use `FoodDiary.Modules.RecentItems.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
