# Meals Contracts

Stable Meal read services, filters, and projection models consumed by Export,
USDA, Favorites, Gamification, WeeklyGoals, WeeklyCheckIn and central reporting.
No infrastructure or application implementation dependencies.

Meals owns IMealNutritionStatisticsReadService and MealNutritionStatisticsBucket; Dashboard adapts them and Cycles/Gamification consume them. IMealAchievementEvaluationRequest is implemented by Gamification. Favorites meal projection contracts belong to Favorites, and USDA nutrition projection ports belong to USDA.

Read contracts reference Meals Domain.Contracts for scalar IDs/enums and have no Favorites contract or aggregate dependency.
