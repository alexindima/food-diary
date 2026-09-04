# MealPlanning application abstractions

Own MealPlans and ShoppingLists ports, errors and persistence projections. Preserve
existing FoodDiary.Application.Abstractions namespaces. Depend only on module
Domain and shared Results. Do not reference central application abstractions,
infrastructure or HTTP. Legacy central Errors partial facades are retired; callers reference these factories directly.
