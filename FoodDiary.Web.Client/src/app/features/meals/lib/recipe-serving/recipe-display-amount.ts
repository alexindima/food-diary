import { type RecipeServings, recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import {
    type RecipeDisplayGrams,
    recipeDisplayGramsFromInput,
    type RecipeServingMass,
    recipeServingMass,
} from '../../../../shared/models/semantics/recipe-quantity';

export type ServingMassResult = { kind: 'known'; gramsPerServing: RecipeServingMass } | { kind: 'unknown' };
export const UNKNOWN_SERVING_MASS: ServingMassResult = { kind: 'unknown' };

export function servingMassFromObservation(gramsPerServing: number | null): ServingMassResult {
    return gramsPerServing !== null && Number.isFinite(gramsPerServing) && gramsPerServing > 0
        ? { kind: 'known', gramsPerServing: recipeServingMass(gramsPerServing) }
        : UNKNOWN_SERVING_MASS;
}

export type RecipeDisplayAmount =
    { unit: 'grams'; value: RecipeDisplayGrams; gramsPerServing: RecipeServingMass } | { unit: 'servings'; value: RecipeServings };

export function displayRecipeServings(servings: RecipeServings, mass: ServingMassResult): RecipeDisplayAmount {
    return mass.kind === 'known'
        ? { unit: 'grams', value: recipeDisplayGramsFromInput(servings * mass.gramsPerServing), gramsPerServing: mass.gramsPerServing }
        : { unit: 'servings', value: servings };
}

export function recipeDisplayToServings(display: RecipeDisplayAmount): RecipeServings {
    return display.unit === 'grams' ? recipeServingsFromStored(display.value / display.gramsPerServing) : display.value;
}

/** Raw form fields use the currently displayed unit; the conversion core always carries that unit. */
export function recipeDisplayFromInput(value: number, mass: ServingMassResult): RecipeDisplayAmount {
    return mass.kind === 'known'
        ? { unit: 'grams', value: recipeDisplayGramsFromInput(value), gramsPerServing: mass.gramsPerServing }
        : { unit: 'servings', value: recipeServingsFromStored(value) };
}
