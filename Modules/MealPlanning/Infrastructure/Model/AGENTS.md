# MealPlanning persistence model

Own all six MealPlans/ShoppingLists EF configurations and explicit model-builder
registration. No dependency on shared DbContext. Preserve legacy configuration
namespaces, tables, indexes, xmin, converters, field access, delete behavior and
the scalar ShoppingList.UserId relationship via `HasOne<User>().WithMany()`.
Ignore transient MealPlanMeal.RecipeSnapshot and keep the scalar Recipe FK.
Source provenance IDs are scalar indexed values, not foreign keys. No new migration
is expected from physical extraction.

Favorites and MealPlanning extend scalar model protection to twenty-two assemblies. Central typed composers preserve six Favorites Cascade FKs and four MealPlanning relationships: optional MealPlan User Cascade, MealPlanMeal Recipe Restrict, ShoppingList User Cascade, and optional ShoppingListItem Product SetNull. Same-owner mappings, indexes, converters and source provenance stay local. Central Infrastructure references Products.Domain explicitly; no schema or API change is intended.
