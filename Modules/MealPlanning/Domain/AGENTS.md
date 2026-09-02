# MealPlanning domain

Own MealPlans and ShoppingLists entities, their IDs, ShoppingLists events and source
enum. Preserve CLR namespaces, invariants and the two explicit aggregate boundaries.
Depend one-way on the Users, Products and Recipes owners and on central Domain for shared value types;
central Domain must not reference this project. No EF or application dependencies.

DietType belongs here in Enums with its existing FoodDiary.Domain.Enums namespace
and member values. MealType belongs to Meals Domain, referenced directly for the
existing planning enum contract. This is a one-way dependency and does not grant
Meals aggregate mutation capabilities. Preserve existing string conversions;
the changed enum assembly owner requires coordinated consumer rebuilds.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
