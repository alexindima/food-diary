# MealPlanning logical module

One physical module owns two explicit aggregate areas: MealPlans and ShoppingLists.
Do not extract ShoppingLists as another business module. Generation crosses the
aggregate boundary through `IShoppingListCreationService`, never its repository.

- Application owns use cases, validation, mappings and read services; preserve its
  `FoodDiary.Application.MealPlanning` assembly and namespaces.
- Application/Abstractions owns the two areas' repository ports, errors and read models.
- Domain owns MealPlan, MealPlanDay, MealPlanMeal and MealPlanDayId with legacy CLR namespaces.
- ShoppingList, its items/sources, IDs, events and enum remain in central Domain:
  public `User.ShoppingLists` and `ShoppingList.User` form a CLR cycle. MealPlanId
  and MealPlanMealId remain central because the source entity uses them.
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

Focused tests live under tests in this module. Mixed HTTP, host, cleanup and
cross-module tests remain central. Run focused tests and central ArchitectureTests;
persistence changes additionally require actual PostgreSQL integration execution
and EF pending-model verification. Do not invoke coverage collectors.
