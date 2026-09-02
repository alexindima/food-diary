# Meals Domain

Own Meal, MealItem, MealAiSession, MealAiItem, their IDs, meal-only states,
nutrition event and AI item/session enums. Preserve existing CLR namespaces and
invariants. Reference central Domain one-way for User/UserId and shared types;
never restore User.Meals or Product/Recipe/Image aggregate navigations.
MeasurementUnit, Visibility, AiRecognitionSource and MealType remain shared.
Keep scalar product/recipe/image IDs and nutrition snapshots unchanged.
