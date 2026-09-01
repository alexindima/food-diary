# MealPlanning Test Guidelines

## Scope

- Own focused MealPlans and ShoppingLists tests within one module; preserve the two aggregate boundaries.
- Application tests retain legacy namespaces and exercise validators, handlers, read models, item building, and `IShoppingListCreationService` orchestration.
- Domain tests cover module-owned MealPlans and ShoppingLists, including their IDs, events, source enum and invariants. The User relationship remains one-way without a central inverse CLR navigation.
- Relational tests use PostgreSQL with production migrations; a Docker skip is not a successful relational verification.
- Mixed domain, shared DbContext, HTTP, host, and cross-module tests remain in central donor projects. Do not duplicate them here.
- Preserve source IDs as scalar provenance, Product deletion as SetNull, and list/item/source cascading deletion.
- Run tests without coverage collectors and preserve TRX counts. Reuse repository-level `.artifacts/mealplanning-shopping-domain` for restore/build/test outputs.
