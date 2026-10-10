import type { MealActions } from "@candidate/app/features/meals/contracts/meal-actions";
import { entityId } from "@candidate/app/shared/models/semantics/entity-id";

declare const meals: MealActions;
const meal = entityId<"meal">("owned");
const product = entityId<"product">("foreign");
meals.deleteById(meal);
// @ts-expect-error A product identity must not authorize a meal mutation.
meals.deleteById(product);
// @ts-expect-error Raw strings must be decoded at the boundary.
meals.deleteById("raw");
// @ts-expect-error A foreign identity must not be restamped silently.
entityId<"meal">(product);
