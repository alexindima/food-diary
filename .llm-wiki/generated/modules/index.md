---
id: generated.application-modules
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Application Modules

This index unifies 34 folder modules and 6 extracted application modules.
Business-module edges, abstraction contracts, adapter consumers, and runtime composition
are reported separately; `none observed` never means proven isolation.

| Module | Role | Business deps | Contract deps | App consumers | Host consumers | Enforcement |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| [Admin](admin.md) | orchestrator | 6 | 7 | 0 | 2 | graph-only |
| [Ai](ai.md) | orchestrator | 0 | 3 | 1 | 2 | graph-only |
| [Authentication](authentication.md) | aggregate-owner | 1 | 3 | 1 | 5 | graph-only |
| [Billing](billing.md) | aggregate-owner | 0 | 1 | 0 | 5 | assembly-isolated |
| [BodyMetrics](body-metrics.md) | aggregate-owner | 0 | 3 | 0 | 4 | project-reference-matrix |
| [Consumptions](consumptions.md) | aggregate-owner | 2 | 7 | 7 | 1 | explicit-boundary-tests |
| [ContentReports](content-reports.md) | aggregate-owner | 0 | 1 | 1 | 1 | graph-only |
| [Cycles](cycles.md) | aggregate-owner | 0 | 2 | 2 | 1 | graph-only |
| [DailyAdvices](daily-advices.md) | aggregate-owner | 0 | 1 | 1 | 1 | graph-only |
| [Dashboard](dashboard.md) | read-composer | 8 | 6 | 0 | 1 | explicit-boundary-tests |
| [Dietologist](dietologist.md) | aggregate-owner | 0 | 0 | 0 | 4 | project-reference-matrix |
| [Email](email.md) | aggregate-owner | 0 | 1 | 2 | 2 | graph-only |
| [Exercises](exercises.md) | aggregate-owner | 0 | 1 | 2 | 1 | graph-only |
| [Export](export.md) | read-composer | 2 | 2 | 0 | 2 | graph-only |
| [Fasting](fasting.md) | aggregate-owner | 0 | 2 | 1 | 2 | explicit-boundary-tests |
| [FavoriteMeals](favorite-meals.md) | aggregate-owner | 1 | 1 | 0 | 1 | graph-only |
| [FavoriteProducts](favorite-products.md) | aggregate-owner | 0 | 2 | 1 | 1 | graph-only |
| [FavoriteRecipes](favorite-recipes.md) | aggregate-owner | 0 | 2 | 1 | 1 | graph-only |
| [Gamification](gamification.md) | read-composer | 1 | 3 | 1 | 1 | graph-only |
| [Hydration](hydration.md) | aggregate-owner | 0 | 1 | 2 | 1 | graph-only |
| [Images](images.md) | aggregate-owner | 0 | 0 | 3 | 3 | explicit-boundary-tests |
| [Lessons](lessons.md) | aggregate-owner | 0 | 2 | 1 | 1 | graph-only |
| [Marketing](marketing.md) | aggregate-owner | 0 | 1 | 0 | 4 | assembly-isolated |
| [MealPlans](meal-plans.md) | aggregate-owner | 1 | 1 | 0 | 1 | graph-only |
| [Notifications](notifications.md) | aggregate-owner | 0 | 0 | 0 | 5 | project-reference-matrix |
| [Nutrition](nutrition.md) | domain-service | 0 | 0 | 2 | 0 | graph-only |
| [OpenFoodFacts](open-food-facts.md) | adapter | 0 | 0 | 1 | 2 | graph-only |
| [Products](products.md) | aggregate-owner | 5 | 5 | 0 | 1 | explicit-boundary-tests |
| [RecentItems](recent-items.md) | aggregate-owner | 0 | 0 | 2 | 0 | graph-only |
| [RecipeComments](recipe-comments.md) | aggregate-owner | 0 | 3 | 0 | 1 | graph-only |
| [RecipeLikes](recipe-likes.md) | aggregate-owner | 0 | 2 | 0 | 1 | graph-only |
| [Recipes](recipes.md) | aggregate-owner | 4 | 4 | 0 | 1 | explicit-boundary-tests |
| [ShoppingLists](shopping-lists.md) | aggregate-owner | 0 | 2 | 1 | 1 | graph-only |
| [Statistics](statistics.md) | read-composer | 0 | 4 | 1 | 1 | graph-only |
| [Tdee](tdee.md) | read-composer | 1 | 3 | 1 | 1 | graph-only |
| [Usda](usda.md) | adapter | 1 | 2 | 1 | 2 | graph-only |
| [Users](users.md) | aggregate-owner | 0 | 0 | 0 | 4 | explicit-boundary-tests |
| [Wearables](wearables.md) | aggregate-owner | 0 | 1 | 0 | 3 | explicit-boundary-tests |
| [WeeklyCheckIn](weekly-check-in.md) | read-composer | 2 | 5 | 0 | 1 | graph-only |
| [WeeklyGoals](weekly-goals.md) | aggregate-owner | 1 | 2 | 0 | 2 | graph-only |
