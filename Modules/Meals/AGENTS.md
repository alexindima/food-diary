# Meals logical module

Meals owns application slices, owner-only repository ports, consumed read contracts,
persistence adapter and explicit EF mappings. Preserve the legacy
`FoodDiary.Application.Meals` assembly identity and all existing CLR namespaces.

`Meal`, `MealItem`, `MealAiSession`, `MealAiItem` and their IDs remain in central
Domain because `User.Meals`, `Product.MealItems`, and `Recipe.MealItems` form a
public bidirectional compatibility graph. Shared `FoodDiaryDbContext`,
`UserConfiguration`, migrations and snapshot remain central. Hosts compose
`AddMealsModule`; JobManager composes `AddMealsPersistence` only.
