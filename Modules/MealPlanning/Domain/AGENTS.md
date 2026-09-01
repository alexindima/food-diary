# MealPlanning domain

Own MealPlans and ShoppingLists entities, their IDs, ShoppingLists events and source
enum. Preserve CLR namespaces, invariants and the two explicit aggregate boundaries.
Depend one-way on central Domain for User, Product, Recipe and shared value types;
central Domain must not reference this project. No EF or application dependencies.
