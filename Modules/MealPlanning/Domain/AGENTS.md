# MealPlanning domain

Own MealPlans and ShoppingLists entities, their IDs, ShoppingLists events and source
enum. Preserve CLR namespaces, invariants and the two explicit aggregate boundaries.
Depend one-way on the Users, Products and Recipes owners and on central Domain for shared value types;
central Domain must not reference this project. No EF or application dependencies.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
