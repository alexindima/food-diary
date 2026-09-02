# Products logical module

Products owns application slices, mutation ports, consumed read/link contracts,
persistence adapters and explicit EF mapping. Follow the narrower layer guides.
Preserve FoodDiary.Application.Products assembly identity and existing CLR namespaces.
Compatibility requires coordinated host rebuilds, not old binary compatibility.

Product, product value objects, FoodQualityScore, FoodQualityGrade and focused invariant tests live in the module Domain with stable CLR namespaces. ProductId, ProductType and MeasurementUnit live in Domain.Contracts, which references only shared primitives. The central and Nutrition domain assemblies are retired. Product keeps one-way User and USDA navigations. RecipeIngredient may reference Product one-way; Users-owned User and MealItem expose no Product CLR navigation. Other modules may reference Products Domain for the existing shared quality formula without acquiring foreign aggregate mutation capability.
Shared context, migrations/snapshot and RecipeCompositionTransactionLock stay central.
See docs/ai/products-ownership-inventory.md for source evidence.

Hosts call AddProductsModule; JobManager composes AddProductsPersistence only.
Provider HTTP/options/cache policy belongs to Integrations/OFF/USDA; image outbox
belongs to Images. Preserve access predicates, row locks, transaction lifetime,
cache invalidation, snapshot nutrition and bounded legacy-row fallback, query ordering/paging and cancellation.

Run module tests, donor/consumer suites, full architecture, HTTP/Swagger and EF
pending-model check; persistence changes require unfiltered PostgreSQL coverage.
