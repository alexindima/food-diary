# MealPlanning ShoppingLists Domain extraction

`Modules/MealPlanning/Domain` physically owns both explicit aggregate areas:
MealPlans and ShoppingLists. The ShoppingLists ownership includes
`ShoppingList`, `ShoppingListItem`, `ShoppingListItemSource`, their strongly typed
IDs, `MealPlanId`, `MealPlanMealId`, ShoppingList domain events and
`ShoppingListItemSourceType`. Existing CLR namespaces and public behavior remain
stable.

The former central CLR cycle was removed because source and test inspection found
no behavioral consumer of `User.ShoppingLists`. `ShoppingList` still holds scalar
`UserId` and the `User` navigation. EF maps that relationship with `WithMany()`,
the same `UserId` foreign key and cascade delete behavior, preserving table, index
and relational schema identity. Historical migrations, the model snapshot,
`FoodDiaryDbContext` DbSets and Users cleanup orchestration remain central.

MealPlanning Domain depends one-way on central Domain for User, Product, Recipe,
their IDs, `MeasurementUnit` and shared primitives. Central Domain does not
reference MealPlanning Domain. Focused ShoppingLists invariants remain in the
module Domain test project; mixed lifecycle, shared-context and HTTP coverage stays
with the central suites.
