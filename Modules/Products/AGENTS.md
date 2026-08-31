# Products logical module

Products owns application slices, mutation ports, consumed read/link contracts,
persistence adapters and explicit EF mapping. Follow the narrower layer guides.
Preserve FoodDiary.Application.Products assembly identity and existing CLR namespaces.
Compatibility requires coordinated host rebuilds, not old binary compatibility.

Product/User/RecipeIngredient/MealItem and USDA navigation graph stays central Domain.
Do not remove public navigations or move foreign aggregates to manufacture isolation.
Shared context, migrations/snapshot and RecipeCompositionTransactionLock stay central.
See docs/ai/products-ownership-inventory.md for source evidence.

Hosts call AddProductsModule; JobManager composes AddProductsPersistence only.
Provider HTTP/options/cache policy belongs to Integrations/OFF/USDA; image outbox
belongs to Images. Preserve access predicates, row locks, transaction lifetime,
cache invalidation, snapshot nutrition, query ordering/paging and cancellation.

Run module tests, donor/consumer suites, full architecture, HTTP/Swagger and EF
pending-model check; persistence changes require unfiltered PostgreSQL coverage.
