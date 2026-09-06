# Meals Domain

Own Meal, MealItem, MealAiSession, MealAiItem, their IDs, meal-only states,
nutrition event and AI item/session enums. Preserve existing CLR namespaces and
invariants. Reference Users Domain.Contracts for UserId and the exact owner of shared types;
never restore User.Meals or Product/Recipe/Image aggregate navigations.
MealType and AiRecognitionSource belong here in Enums with unchanged
FoodDiary.Domain.Enums namespaces and member values. Consumers reference this
owner explicitly; moving the assembly requires coordinated consumer rebuilds.
MeasurementUnit belongs to Products Domain.Contracts, referenced directly; Visibility belongs to shared Primitives.
Keep scalar product/recipe/image IDs and nutrition snapshots unchanged.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access.
