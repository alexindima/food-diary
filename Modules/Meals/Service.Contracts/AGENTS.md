# Meals consumer contracts

Own GetMealsQuery and its MealModel, MealItemModel, MealAiSessionModel and MealAiItemModel projections consumed by Dashboard. Meals.Contracts remains the existing daily-calorie/ID seam. Keep handlers, aggregate policy and MealOverviewModel in Application.

Preserve existing CLR namespaces and wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.
