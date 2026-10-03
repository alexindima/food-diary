import type { Product } from '../../../models/product.data';
import type { Recipe } from '../../../models/recipe.data';

export type ItemSelection = { type: 'Product'; product: Product } | { type: 'Recipe'; recipe: Recipe } | { type: 'Text'; name: string };

export type ItemSelectDialogData = {
    allowText?: boolean;
    initialTab?: 'Product' | 'Recipe';
    lockInitialTab?: boolean;
    excludedRecipeId?: string | null;
};
