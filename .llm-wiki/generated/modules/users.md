---
id: generated.module.users
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Users

## Graph

- Origin: module-graph
- Dependencies: Images
- Consumers: Admin, Ai, Authentication, Consumptions, ContentReports, Cycles, DailyAdvices, Dashboard, Dietologist, Exercises, Export, Fasting, FavoriteMeals, FavoriteProducts, FavoriteRecipes, Gamification, Hydration, Lessons, MealPlans, Notifications, Products, RecipeComments, RecipeLikes, Recipes, ShoppingLists, Statistics, Tdee, Usda, WaistEntries, Wearables, WeeklyCheckIn, WeeklyGoals, WeightEntries

## Source Areas

- `FoodDiary.Application.Abstractions/Users`
- `FoodDiary.Application/Users`
- `FoodDiary.Domain/Entities/Users`
- `FoodDiary.Infrastructure/Persistence/Configurations/Users`
- `FoodDiary.Infrastructure/Persistence/Users`
- `FoodDiary.Presentation.Api/Features/Users`
- `tests/FoodDiary.Application.Tests/Users`

## HTTP Surface

### UserAiConsentController

Source: `FoodDiary.Presentation.Api/Features/Users/UserAiConsentController.cs`

- `POST /api/v{version:apiVersion}/users/ai-consent`
- `DELETE /api/v{version:apiVersion}/users/ai-consent`

### UserOverviewController

Source: `FoodDiary.Presentation.Api/Features/Users/UserOverviewController.cs`

- `GET /api/v{version:apiVersion}/users/overview`

### UsersController

Source: `FoodDiary.Presentation.Api/Features/Users/UsersController.cs`

- `GET /api/v{version:apiVersion}/users/info`
- `PATCH /api/v{version:apiVersion}/users/info`
- `PATCH /api/v{version:apiVersion}/users/preferences/appearance`
- `GET /api/v{version:apiVersion}/users/desired-weight`
- `PUT /api/v{version:apiVersion}/users/desired-weight`
- `GET /api/v{version:apiVersion}/users/desired-waist`
- `PUT /api/v{version:apiVersion}/users/desired-waist`
- `DELETE /api/v{version:apiVersion}/users`

### UsersPasswordController

Source: `FoodDiary.Presentation.Api/Features/Users/UsersPasswordController.cs`

- `PATCH /api/v{version:apiVersion}/users/password`
- `PATCH /api/v{version:apiVersion}/users/password/set`

### WaistGoalsController

Source: `FoodDiary.Presentation.Api/Features/Users/WaistGoalsController.cs`

- `GET /api/v{version:apiVersion}/users/waist-goals`

### WeightGoalsController

Source: `FoodDiary.Presentation.Api/Features/Users/WeightGoalsController.cs`

- `GET /api/v{version:apiVersion}/users/weight-goals`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Users/AiConsentTests.cs`
- `tests/FoodDiary.Application.Tests/Users/CurrentUserAccessPolicyTests.cs`
- `tests/FoodDiary.Application.Tests/Users/HistoryPageSummaryHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Users/HistoryProfileCoverageTests.cs`
- `tests/FoodDiary.Application.Tests/Users/UpdateUserCommandHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Users/UserApplicationServiceDelegationTests.cs`
- `tests/FoodDiary.Application.Tests/Users/UsersFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Users/UsersValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/UsersControllerTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/UsersPasswordControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
