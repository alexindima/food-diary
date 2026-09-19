# Meals Contracts

Stable Meal read services, filters, and projection models consumed by Export,
USDA, Favorites, Gamification, WeeklyGoals, WeeklyCheckIn and central reporting.
No infrastructure or application implementation dependencies.

Meals owns IMealNutritionStatisticsReadService, MealNutritionStatisticsBucket and ReadMealNutritionStatisticsQuery. Statistics and WeeklyCheckIn dispatch the query; Dashboard adapts the capability and Cycles/Gamification consume it. Callers retain authorization and date validation. IMealAchievementEvaluationRequest is implemented by Gamification. Favorites meal projection contracts belong to Favorites, and USDA nutrition projection ports belong to USDA.

Read contracts reference Meals Domain.Contracts for scalar IDs/enums and have no Favorites contract or aggregate dependency.

IMealItemDisplayReadService returns immutable, tenant-scoped batch item display models for Dashboard. Consumers must not duplicate snapshot fallback or product-quality policy.
