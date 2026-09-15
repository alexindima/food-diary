# RecentItems Module

RecentItems owns the recent-product/recent-recipe usage aggregate, narrow usage contracts, persistence adapter, post-commit recorder and EF mapping. Use canonical project and folder namespaces. Its purge uses the owner context and live shared transaction. Migrations/snapshot, post-commit queue/UoW and Users cleanup orchestration remain shared seams; this adapter no longer references central Infrastructure.

Application owns ReadRecentProductsQuery and ReadRecentRecipesQuery handlers. Contracts exposes these reads through ISender; repository ports remain in Application.Abstractions. The post-commit usage recorder remains a technical lifecycle port.

Current module convention: all projects use `FoodDiary.Modules.RecentItems.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
