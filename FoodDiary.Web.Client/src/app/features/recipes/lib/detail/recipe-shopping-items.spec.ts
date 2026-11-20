import { describe, expect, it, vi } from 'vitest';

import { type Recipe, type RecipeIngredient, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../shared/models/recipe-ingredient';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { buildRecipeShoppingItems } from './recipe-shopping-items';

const MAXIMUM_LIST_AMOUNT = 1_000_000;
const FRACTIONAL_SERVINGS = 1.5;

describe('Recipe shopping-list ingredient projection', () => {
    it('preserves the ingredient indices across steps when skipping an unavailable nameless item', () => {
        const beans = recipeIngredientFromStored({
            id: 'beans',
            productId: 'product-beans',
            productName: '  Dried beans  ',
            amount: 2,
        });
        const unavailable = recipeIngredientFromStored({ id: 'missing', amount: 1 });
        const oldDressing = recipeIngredientFromStored({ id: 'dressing', nestedRecipeName: 'Herb dressing', amount: 1 });
        const translate = vi.fn<(key: string) => string>();

        const rows = buildRecipeShoppingItems(withSteps([beans, unavailable], [oldDressing]), translate);

        expect(rows).toEqual([
            { index: 0, item: { name: 'Dried beans', isChecked: false, productId: 'product-beans', amount: 2, unit: null } },
            { index: 2, item: { name: 'Herb dressing', isChecked: false, note: null } },
        ]);
        expect(translate).not.toHaveBeenCalled();
    });

    it('keeps editable text items and localizes fractional nested recipe servings', () => {
        const herbs = recipeIngredientFromStored({ id: 'herbs', textName: '  Parsley  ', amount: 0 });
        const pepper = recipeIngredientFromStored({ id: 'pepper', textName: 'Black pepper', amountText: '  to taste  ', amount: 0 });
        const starter = recipeIngredientFromStored({
            id: 'starter',
            nestedRecipeId: 'recipe-starter',
            nestedRecipeName: 'Sourdough starter',
            amount: FRACTIONAL_SERVINGS,
        });
        const translate = vi.fn<(key: string) => string>().mockReturnValue('servings');

        const rows = buildRecipeShoppingItems(withSteps([herbs, pepper, starter]), translate);

        expect(rows).toEqual([
            { index: 0, item: { name: 'Parsley', isChecked: false, note: null } },
            { index: 1, item: { name: 'Black pepper', isChecked: false, note: 'to taste' } },
            { index: 2, item: { name: 'Sourdough starter', isChecked: false, note: '1.5 servings' } },
        ]);
        expect(translate).toHaveBeenCalledExactlyOnceWith('RECIPE_DETAIL.SUMMARY.SERVINGS_FEW');
    });

    it('keeps the shopping amount limit and omits out-of-range quantities from stored recipes', () => {
        const accepted = recipeIngredientFromStored({
            id: 'oats',
            productId: 'product-oats',
            productName: 'Oats',
            productBaseUnit: 'g',
            amount: MAXIMUM_LIST_AMOUNT,
        });
        const unmeasured = recipeIngredientFromStored({ id: 'salt', productId: 'product-salt', productName: 'Salt', amount: 0 });
        const excessive = recipeIngredientFromStored({
            id: 'stock',
            nestedRecipeId: 'recipe-stock',
            nestedRecipeName: 'Vegetable stock',
            amount: MAXIMUM_LIST_AMOUNT + 1,
        });
        const translate = vi.fn<(key: string) => string>();

        const rows = buildRecipeShoppingItems(withSteps([accepted, unmeasured, excessive]), translate);

        expect(rows).toEqual([
            { index: 0, item: { name: 'Oats', isChecked: false, productId: 'product-oats', amount: MAXIMUM_LIST_AMOUNT, unit: 'g' } },
            { index: 1, item: { name: 'Salt', isChecked: false, productId: 'product-salt', amount: null, unit: null } },
            { index: 2, item: { name: 'Vegetable stock', isChecked: false, note: null } },
        ]);
        expect(translate).not.toHaveBeenCalled();
    });
});

function withSteps(...steps: RecipeIngredient[][]): Recipe {
    return {
        id: entityId<'recipe'>('meal-prep'),
        name: 'Bean stew',
        servings: 2,
        usageCount: 0,
        visibility: RecipeVisibility.Private,
        createdAt: utcInstant('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: steps.map((ingredients, index) => ({
            id: `step-${index + 1}`,
            stepNumber: index + 1,
            instruction: 'Prepare the ingredients.',
            ingredients,
        })),
    };
}
