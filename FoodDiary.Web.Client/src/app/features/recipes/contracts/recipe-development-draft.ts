import type { Product } from '../../../shared/models/product.data';
import type { Recipe } from '../../../shared/models/recipe.data';

export type RecipeDevelopmentIngredient = { product: Product; amount: number; recipe?: never } | { recipe: Recipe; amount: number; product?: never };

/** Resolved by application composition before handing data to the live recipe form. */
export class RecipeDevelopmentDraft {
    public constructor(
        public readonly name: string,
        public readonly servings: number,
        public readonly steps: Array<{ description: string; ingredients: RecipeDevelopmentIngredient[] }>,
        public readonly cookTime = 1,
    ) {}
}
