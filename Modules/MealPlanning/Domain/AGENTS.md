# MealPlanning domain

Own MealPlans entities and MealPlanDayId. Preserve CLR namespaces, invariants and
aggregate boundaries. Depend one-way on central Domain for User/Recipe and shared
IDs. Do not move central ShoppingList graph or MealPlanId/MealPlanMealId without
resolving the documented public CLR cycle. No EF or application dependencies.
