# Presentation module inventory

| Module | Owned feature folders |
| --- | --- |
| Admin | Admin |
| Ai | Ai |
| Billing | Billing |
| BodyMetrics | WaistEntries, WeightEntries |
| ContentReports | ContentReports |
| Cycles | Cycles |
| Dashboard | Dashboard |
| Dietologist | Dietologist |
| Exercises | Exercises |
| Export | Export |
| Fasting | Fasting, Logs |
| Favorites | FavoriteMeals, FavoriteProducts, FavoriteRecipes |
| Gamification | Gamification |
| Hydration | Hydration |
| Identity | Auth |
| Images | Images |
| Lessons | Lessons |
| Marketing | Marketing |
| MealPlanning | MealPlans, ShoppingLists |
| Meals | Meals |
| Notifications | Notifications |
| OpenFoodFacts | OpenFoodFacts |
| Products | Products |
| RecipeCommunity | RecipeComments, RecipeLikes |
| Recipes | Recipes |
| Statistics | Statistics |
| Tdee | Tdee |
| Usda | Usda |
| Users | Goals, Users |
| Wearables | Wearables |
| WeeklyCheckIn | WeeklyCheckIn |
| WeeklyGoals | WeeklyGoals |

`FoodDiary.Presentation.Api/Features/Version` is intentionally central because it is a version-neutral host API endpoint rather than a business-module adapter. DailyAdvices is consumed through Dashboard and RecentItems through other module flows; neither currently has a dedicated HTTP feature folder.
