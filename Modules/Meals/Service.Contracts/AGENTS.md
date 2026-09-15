# Meals consumer contracts

Own GetMealsQuery and its MealModel, MealItemModel, MealAiSessionModel and MealAiItemModel projections consumed by Dashboard. Meals.Contracts remains the existing daily-calorie/ID seam. Keep handlers, aggregate policy and MealOverviewModel in Application.

Use canonical project and folder namespaces and preserve wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.
