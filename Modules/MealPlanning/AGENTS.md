# MealPlanning logical module

One physical module owns two explicit aggregate areas: MealPlans and ShoppingLists.
Do not extract ShoppingLists as another business module. Generation crosses the
aggregate boundary through `IShoppingListCreationService`, never its repository.

- Application owns use cases, validation and mappings with canonical assembly and folder namespaces. Keep single-operation reads in their handlers.
- Application.Abstractions owns the two areas' repository ports, errors and read models.
- Domain owns MealPlan, MealPlanDay, MealPlanMeal, ShoppingList, its items/sources,
  their IDs, events and enum with canonical project and folder namespaces. MealPlanId and
  MealPlanMealId are module-owned source-provenance IDs.
- Infrastructure owns both repositories and complete `AddMealPlanningModule` DI.
- PersistenceModel owns all six mappings; shared DbContext explicitly applies
  them. Historical migrations and the model snapshot remain central.
- HTTP controllers live in Presentation/MealPlans/Controllers and Presentation/ShoppingLists/Controllers.

Preserve user scoping, cancellation, request transaction ownership, tracked update
semantics, CreatedOnUtc ordering and source-aware item projections. Generation
aggregates by ProductId, scales servings, rounds amounts ToEven and creates a new
list on every invocation; it does not merge into an existing list. Preserve Product
SetNull, Recipe Restrict, list/item/source cascade and scalar source IDs without
MealPlan/Recipe foreign keys.

The ShoppingList-to-User relationship is deliberately one-way: preserve scalar
`UserId` and schema-equivalent `HasOne<User>().WithMany()`;
do not restore a central `User.ShoppingLists` CLR navigation.

Focused tests live under tests in this module. Mixed HTTP, host, cleanup and
cross-module tests remain central. Run focused tests and central ArchitectureTests;
persistence changes additionally require actual PostgreSQL integration execution
and EF pending-model verification. Do not invoke coverage collectors.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

Keep MealPlans and ShoppingLists as meaningful areas. Query handlers own their read operations. IShoppingListCreationService remains an aggregate boundary for generation, not a single-use read facade.
