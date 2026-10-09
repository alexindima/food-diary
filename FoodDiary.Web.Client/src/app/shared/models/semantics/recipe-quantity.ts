/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- constructors attach quantity meanings without changing stored observations */
import type { SemanticQuantity, UnbrandedQuantity } from './quantity-meaning';

export type ProductIngredientAmount = SemanticQuantity<'product-ingredient-amount'>;
export type NestedRecipeIngredientServings = SemanticQuantity<'nested-recipe-ingredient-servings'>;
export type ObservedIngredientAmount = SemanticQuantity<'observed-ingredient-amount'>;
export type RecipeDisplayGrams = SemanticQuantity<'recipe-display-grams'>;
export type RecipeServingMass = SemanticQuantity<'recipe-grams-per-serving'>;

/** Ingredient projections retain historical values; consumption limits do not apply here. */
export function productIngredientAmountFromStored(value: UnbrandedQuantity | ProductIngredientAmount): ProductIngredientAmount {
    return value as ProductIngredientAmount;
}

export function nestedRecipeIngredientServingsFromStored(
    value: UnbrandedQuantity | NestedRecipeIngredientServings,
): NestedRecipeIngredientServings {
    return value as NestedRecipeIngredientServings;
}

export function observedIngredientAmountFromStored(value: UnbrandedQuantity | ObservedIngredientAmount): ObservedIngredientAmount {
    return value as ObservedIngredientAmount;
}

/** A raw form amount may be zero or invalid while the user edits it. */
export function recipeDisplayGramsFromInput(value: UnbrandedQuantity | RecipeDisplayGrams): RecipeDisplayGrams {
    return value as RecipeDisplayGrams;
}

export function recipeServingMass(value: UnbrandedQuantity | RecipeServingMass): RecipeServingMass {
    if (!Number.isFinite(value) || value <= 0) {
        throw new RangeError('Serving mass must be finite positive grams per serving.');
    }
    return value as RecipeServingMass;
}
