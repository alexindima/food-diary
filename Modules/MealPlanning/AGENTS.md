# MealPlanning logical module

One physical module owns two explicit aggregate areas: MealPlans and ShoppingLists.
Do not extract ShoppingLists as another business module. Generation crosses the
aggregate boundary through `IShoppingListCreationService`, never its repository.

- Application owns use cases, validation, mappings and read services; preserve its
  `FoodDiary.Application.MealPlanning` assembly and namespaces.
- Application/Abstractions owns the two areas' repository ports, errors and read models.
- Domain owns MealPlan, MealPlanDay, MealPlanMeal, ShoppingList, its items/sources,
  their IDs, events and enum with legacy CLR namespaces. MealPlanId and
  MealPlanMealId are module-owned source-provenance IDs.
- Infrastructure owns both repositories and complete `AddMealPlanningModule` DI.
- Infrastructure/Model owns all six mappings; shared DbContext explicitly applies
  them. Historical migrations and the model snapshot remain central.
- HTTP controllers remain in Presentation.Api/Features/MealPlans and ShoppingLists.

Preserve user scoping, cancellation, request transaction ownership, tracked update
semantics, CreatedOnUtc ordering and source-aware item projections. Generation
aggregates by ProductId, scales servings, rounds amounts ToEven and creates a new
list on every invocation; it does not merge into an existing list. Preserve Product
SetNull, Recipe Restrict, list/item/source cascade and scalar source IDs without
MealPlan/Recipe foreign keys.

The ShoppingList-to-User relationship is deliberately one-way: preserve scalar
`UserId`, the `ShoppingList.User` navigation and schema-equivalent `WithMany()`;
do not restore a central `User.ShoppingLists` CLR navigation.

Focused tests live under tests in this module. Mixed HTTP, host, cleanup and
cross-module tests remain central. Run focused tests and central ArchitectureTests;
persistence changes additionally require actual PostgreSQL integration execution
and EF pending-model verification. Do not invoke coverage collectors.
