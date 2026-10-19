import { InjectionToken } from '@angular/core';

import type { Product } from '../../../shared/models/product.data';
import type { Recipe } from '../../../shared/models/recipe.data';
import type { RecipeServings } from '../../../shared/models/semantics/meal-quantity';

/** Optional, local-development integration. No persistence or submit capability. */
export type MealDevelopmentDraft = {
    date: string;
    time: string;
    mealType: string;
    comment: string;
    items: Array<
        | { product: Product; amount: number; recipe?: never; servings?: never }
        | { recipe: Recipe; servings: RecipeServings; product?: never; amount?: never }
    >;
};

export type MealDevelopmentScreen = {
    read: () => unknown;
    isDirty?: () => boolean;
    fillDraft?: (draft: MealDevelopmentDraft, requireSession: () => void) => void | Promise<void>;
    clearDraft?: () => void;
};

export type MealDevelopmentTools = {
    attach: (screen: MealDevelopmentScreen) => () => void;
};

export const MEAL_DEVELOPMENT_TOOLS = new InjectionToken<MealDevelopmentTools>('MEAL_DEVELOPMENT_TOOLS');
