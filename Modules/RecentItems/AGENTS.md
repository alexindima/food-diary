# RecentItems Module

RecentItems owns the recent-product/recent-recipe usage aggregate, narrow usage contracts, persistence adapter, post-commit recorder and EF mapping. Keep CLR namespaces stable. Central `FoodDiaryDbContext`, migrations/snapshot, post-commit queue/UoW and Users cleanup orchestration remain shared seams.

Do not add an Application project unless the module gains genuine handlers/use cases. Products, Recipes and Meals consume only the narrow abstractions.
