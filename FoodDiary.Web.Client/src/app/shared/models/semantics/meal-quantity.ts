/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- these constructors attach a phantom meaning without changing scalar wire values */
import type { SemanticQuantity, UnbrandedQuantity } from './quantity-meaning';

export type ProductQuantity = SemanticQuantity<'product-quantity'>;
export type RecipeServings = SemanticQuantity<'recipe-servings'>;

const MAXIMUM_CONSUMED_QUANTITY = 1_000_000;

function validateConsumption(value: number): void {
    if (!Number.isFinite(value) || value <= 0 || value > MAXIMUM_CONSUMED_QUANTITY) {
        throw new RangeError('Consumed quantity must be finite and in (0, 1000000].');
    }
}

export function productQuantity(value: UnbrandedQuantity | ProductQuantity): ProductQuantity {
    validateConsumption(value);
    return productQuantityFromStored(value);
}

export function recipeServings(value: UnbrandedQuantity | RecipeServings): RecipeServings {
    validateConsumption(value);
    return recipeServingsFromStored(value);
}

/** Historical projections keep their existing values and fallback behavior. */
export function productQuantityFromStored(value: UnbrandedQuantity | ProductQuantity): ProductQuantity {
    return value as ProductQuantity;
}

export function recipeServingsFromStored(value: UnbrandedQuantity | RecipeServings): RecipeServings {
    return value as RecipeServings;
}
