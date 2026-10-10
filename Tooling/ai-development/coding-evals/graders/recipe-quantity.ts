import {
    displayRecipeServings,
    recipeDisplayToServings,
    UNKNOWN_SERVING_MASS,
} from "@candidate/app/features/meals/lib/recipe-serving/recipe-display-amount";
import { recipeServings } from "@candidate/app/shared/models/semantics/meal-quantity";
import {
    recipeDisplayGramsFromInput,
    recipeServingMass,
} from "@candidate/app/shared/models/semantics/recipe-quantity";

const servings = recipeServings(1);
const grams = recipeDisplayGramsFromInput(100);
const mass = recipeServingMass(50);
displayRecipeServings(servings, UNKNOWN_SERVING_MASS);
recipeDisplayToServings({ unit: "grams", value: grams, gramsPerServing: mass });
// @ts-expect-error Display grams are not servings.
displayRecipeServings(grams, UNKNOWN_SERVING_MASS);
// @ts-expect-error Grams require their conversion mass.
recipeDisplayToServings({ unit: "grams", value: grams });
// @ts-expect-error Conversion mass is not the consumed amount.
recipeDisplayToServings({ unit: "grams", value: mass, gramsPerServing: grams });
