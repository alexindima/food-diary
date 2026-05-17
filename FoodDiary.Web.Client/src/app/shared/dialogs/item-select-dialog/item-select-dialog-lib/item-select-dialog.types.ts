import type { Product } from '../../../../features/products/models/product.data';
import type { Recipe } from '../../../../features/recipes/models/recipe.data';

export type ItemSelection = { type: 'Product'; product: Product } | { type: 'Recipe'; recipe: Recipe };

export type ItemSelectDialogData = {
    initialTab?: 'Product' | 'Recipe';
};
