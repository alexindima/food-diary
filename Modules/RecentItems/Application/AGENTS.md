# RecentItems application

Own ReadRecentProductsQuery and ReadRecentRecipesQuery handlers. Public requests and immutable results belong to Contracts; handlers read only the owner IRecentItemReadRepository. Register through AddRecentItemsApplication and the Infrastructure module facade.

Preserve user scoping, limits, ordering and cancellation. These reads do not save or begin transactions. Post-commit usage recording remains an independent technical lifecycle adapter in Infrastructure.
