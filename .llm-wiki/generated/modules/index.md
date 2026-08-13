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

This index unifies 4 folder modules and 30 extracted application modules.
Business-module edges, abstraction contracts, adapter consumers, and runtime composition
are reported separately; `none observed` never means proven isolation.

| Module | Role | Business deps | Contract deps | App consumers | Host consumers | Enforcement |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| [Admin](admin.md) | orchestrator | 4 | 7 | 0 | 2 | graph-only |
| [Ai](ai.md) | orchestrator | 0 | 3 | 1 | 6 | project-reference-matrix |
| [Billing](billing.md) | aggregate-owner | 0 | 1 | 0 | 5 | assembly-isolated |
| [BodyMetrics](body-metrics.md) | aggregate-owner | 0 | 3 | 0 | 4 | project-reference-matrix |
| [ContentReports](content-reports.md) | aggregate-owner | 0 | 1 | 1 | 5 | project-reference-matrix |
| [Cycles](cycles.md) | aggregate-owner | 0 | 2 | 0 | 7 | project-reference-matrix |
| [DailyAdvices](daily-advices.md) | read-composer | 0 | 1 | 0 | 6 | project-reference-matrix |
| [Dashboard](dashboard.md) | read-composer | 0 | 0 | 0 | 4 | explicit-boundary-tests |
| [Dietologist](dietologist.md) | aggregate-owner | 0 | 0 | 0 | 4 | project-reference-matrix |
| [Exercises](exercises.md) | aggregate-owner | 0 | 1 | 0 | 7 | project-reference-matrix |
| [Export](export.md) | read-composer | 0 | 2 | 0 | 4 | project-reference-matrix |
| [Fasting](fasting.md) | aggregate-owner | 0 | 2 | 0 | 4 | project-reference-matrix |
| [Favorites](favorites.md) | aggregate-owner | 0 | 7 | 0 | 4 | project-reference-matrix |
| [Gamification](gamification.md) | read-composer | 0 | 3 | 1 | 5 | project-reference-matrix |
| [Hydration](hydration.md) | aggregate-owner | 0 | 1 | 0 | 7 | project-reference-matrix |
| [Identity](identity.md) | aggregate-owner | 0 | 5 | 0 | 4 | assembly-isolated |
| [Images](images.md) | aggregate-owner | 0 | 0 | 2 | 7 | project-reference-matrix |
| [Lessons](lessons.md) | aggregate-owner | 0 | 2 | 1 | 5 | project-reference-matrix |
| [Marketing](marketing.md) | aggregate-owner | 0 | 1 | 0 | 4 | assembly-isolated |
| [MealPlanning](meal-planning.md) | aggregate-owner | 0 | 4 | 0 | 4 | project-reference-matrix |
| [Meals](meals.md) | aggregate-owner | 0 | 8 | 0 | 11 | project-reference-matrix |
| [Notifications](notifications.md) | aggregate-owner | 0 | 0 | 0 | 5 | project-reference-matrix |
| [OpenFoodFacts](open-food-facts.md) | adapter | 0 | 0 | 1 | 6 | project-reference-matrix |
| [Products](products.md) | aggregate-owner | 4 | 6 | 0 | 1 | explicit-boundary-tests |
| [RecentItems](recent-items.md) | aggregate-owner | 0 | 0 | 2 | 0 | graph-only |
| [RecipeCommunity](recipe-community.md) | aggregate-owner | 0 | 5 | 0 | 4 | project-reference-matrix |
| [Recipes](recipes.md) | aggregate-owner | 2 | 6 | 0 | 1 | explicit-boundary-tests |
| [Statistics](statistics.md) | read-composer | 0 | 4 | 0 | 6 | project-reference-matrix |
| [Tdee](tdee.md) | read-composer | 0 | 3 | 0 | 6 | project-reference-matrix |
| [Usda](usda.md) | adapter | 0 | 2 | 1 | 6 | project-reference-matrix |
| [Users](users.md) | aggregate-owner | 0 | 0 | 0 | 4 | explicit-boundary-tests |
| [Wearables](wearables.md) | aggregate-owner | 0 | 1 | 0 | 5 | assembly-isolated |
| [WeeklyCheckIn](weekly-check-in.md) | read-composer | 0 | 5 | 0 | 4 | project-reference-matrix |
| [WeeklyGoals](weekly-goals.md) | aggregate-owner | 0 | 2 | 0 | 4 | project-reference-matrix |
