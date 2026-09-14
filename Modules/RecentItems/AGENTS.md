# RecentItems Module

RecentItems owns the recent-product/recent-recipe usage aggregate, narrow usage contracts, persistence adapter, post-commit recorder and EF mapping. Keep CLR namespaces stable. Its purge uses the owner context and live shared transaction. Migrations/snapshot, post-commit queue/UoW and Users cleanup orchestration remain shared seams; this adapter no longer references central Infrastructure.

Do not add an Application project unless the module gains genuine handlers/use cases. Products, Recipes and Meals consume only the narrow Contracts project; internal repository ports stay in Application/Abstractions.
