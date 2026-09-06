# Recipes logical module

Recipes owns the Recipe aggregate, steps, ingredients, recipe-only value objects/events, and focused domain tests. Keep stable `FoodDiary.Domain.*` namespaces. Recipe IDs are owned by dependency-free `Domain.Contracts`; Consumers reference that ID seam directly. Recipes Domain references Users and Products owners, its own Domain.Contracts and shared Primitives. Do not restore User.Recipes, Product.RecipeIngredients, Recipe.MealItems, or MealItem.Recipe inverse CLR navigations; preserve their database relationships through explicit unidirectional EF mappings.

Own recipe use cases, aggregate ports, projection contracts, EF mappings and adapters. Preserve legacy Application assembly and CLR namespaces. RecipeCommunity stays a separate owner. Remove foreign CLR navigations only with explicit schema-equivalent foreign keys and verified owner projections; preserve FK/delete/schema semantics.

Central DbContext/DbSets/migrations/snapshot remain central; explicitly apply ApplyRecipesPersistenceModel. Shared RecipeCompositionTransactionLock remains central with narrowly granted friend access for Recipes infrastructure and unchanged Products coordination. Hosts call AddRecipesModule; JobManager calls AddRecipesPersistence only, preserving its application registration set.

Keep nested recipe/cycle validation, nutrition/rounding, servings/unit conversion, visibility/access, mutation transactions and media/outbox calls unchanged. Focused tests live in tests under this module. Mixed PostgreSQL, Domain Meal snapshots, HTTP and DI suites stay central. See docs/ai/recipes-ownership-inventory.md. Use one .artifacts/recipes-extraction build scope and separate .artifacts/recipes-extraction-results evidence. No coverage collectors.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
