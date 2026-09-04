# Meals logical module

Meals owns Meal, MealItem, MealAiSession, MealAiItem, their IDs, meal-only states,
nutrition event and AI item/session enums under `Modules/Meals/Domain`, with stable
CLR namespaces. User has no inverse Meals collection; Meal.User remains a one-way
relationship with the same required FK and cascade. Shared User/UserId, enums,
DbContext, migrations and snapshot remain with their existing owners. Product,
Recipe and Image links remain ID-based with unchanged batch snapshot fallbacks.
No extra Domain.Contracts project is needed by the current acyclic graph.
See `docs/ai/meals-ownership-inventory.md` for source evidence and remaining seams.

Preserve the legacy FoodDiary.Application.Meals assembly identity. Hosts compose
AddMealsModule; JobManager composes AddMealsPersistence only.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
