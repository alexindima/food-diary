# Meals Domain

Own Meal, MealItem, MealAiSession, MealAiItem, their IDs, meal-only states,
nutrition event and AI item/session enums. Preserve existing CLR namespaces and
invariants. Reference central Domain one-way for User/UserId and shared types;
never restore User.Meals or Product/Recipe/Image aggregate navigations.
MealType and AiRecognitionSource belong here in Enums with unchanged
FoodDiary.Domain.Enums namespaces and member values. Consumers reference this
owner explicitly; moving the assembly requires coordinated consumer rebuilds.
MeasurementUnit belongs to Shared/FoodDiary.Nutrition.Domain, referenced directly; Visibility remains in residual central Domain.
Keep scalar product/recipe/image IDs and nutrition snapshots unchanged.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access.
