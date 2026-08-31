# Recipes logical module

Own recipe use cases, aggregate ports, projection contracts, EF mappings and adapters. Preserve legacy Application assembly and CLR namespaces. No Recipes.Domain project: central User.Recipes, Recipe.MealItems, MealItem.Recipe/ApplyRecipeSnapshot and Product.RecipeIngredients are public compatibility seams. RecipeCommunity stays a separate owner. Never remove navigations or change FK/delete/schema semantics to force isolation.

Central DbContext/DbSets/migrations/snapshot remain central; explicitly apply ApplyRecipesPersistenceModel. Shared RecipeCompositionTransactionLock remains central with narrowly granted friend access for Recipes infrastructure and unchanged Products coordination. Hosts call AddRecipesModule; JobManager calls AddRecipesPersistence only, preserving its application registration set.

Keep nested recipe/cycle validation, nutrition/rounding, servings/unit conversion, visibility/access, mutation transactions and media/outbox calls unchanged. Focused tests live in tests under this module. Mixed PostgreSQL, Domain Meal snapshots, HTTP and DI suites stay central. See docs/ai/recipes-ownership-inventory.md. Use one .artifacts/recipes-extraction build scope and separate .artifacts/recipes-extraction-results evidence. No coverage collectors.
