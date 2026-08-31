# MealPlanning persistence adapters

Own MealPlans and ShoppingLists repositories and complete AddMealPlanningModule
registration. Preserve existing repository CLR namespaces for in-repository
consumers. Use shared FoodDiaryDbContext; do not save or begin a transaction inside
these repositories. Keep user filters and tracking/projection behavior unchanged.
Central Infrastructure references Model, never this adapter project.
