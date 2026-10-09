# MealPlanning domain

Own MealPlans and ShoppingLists entities, their IDs, ShoppingLists events and source
enum. Use canonical project and folder namespaces; preserve invariants and the two explicit aggregate boundaries.
Depend on Users/Products/Recipes Domain.Contracts, the Meals enum owner and shared Primitives
through the exact references in this project. No EF or application dependencies.

DietType belongs here in Enums with its existing FoodDiary.Domain.Enums namespace
and member values. MealType belongs to Meals Domain.Contracts, referenced directly for the
existing planning enum contract. This is a one-way dependency and does not grant
Meals aggregate mutation capabilities. Preserve existing string conversions;
the changed enum assembly owner requires coordinated consumer rebuilds.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.

MealPlanMeal retains RecipeId and a transient immutable RecipeSnapshot. Snapshot
assignment validates its recipe ID and copies the ingredient collection. The owner
repository batch-projects recipe/product data; domain objects never retain their
mutable aggregates. EF ignores RecipeSnapshot and preserves the original Recipe FK.

MealType is owned by Meals Domain.Contracts; the planning domain has no Meals aggregate assembly reference.

ShoppingQuantity groups optional amount and unit while retaining every absence combination, enum validation and the positive finite amount limit of 1,000,000. ShoppingSourceQuantity deliberately has its separate positive finite unbounded amount contract and retains historical source unit values. Mutations use AddItemWithQuantity/CreateWithQuantity/UpdateWithQuantity/AddMealPlanSourceWithQuantity. Preserve free-text items, source provenance, sorting and partial field update behavior; no blanket quantity limit or unit conversion.

PlanDurationDays and PlanDayNumber retain1..31 bounds and the aggregate duration-relative day check. PlannedServings is a positive integral count with no new cap; do not substitute fractional RecipeServingQuantity. Catalog mutation paths consume these owner values; persisted plan fields and adopted-plan copying remain compatible.
