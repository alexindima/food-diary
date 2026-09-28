import { describe, expect, it } from 'vitest';

import { resolveIngredientUnitKey, resolveServingsUnitKey } from './recipe-servings.utils';

const FIVE = 5;
const ELEVEN = 11;
const TWENTY_ONE = 21;
const HALF = 0.5;
const ONE_AND_HALF = 1.5;
const CASES = [
    [1, 'ONE'],
    [2, 'FEW'],
    [FIVE, 'MANY'],
    [ELEVEN, 'MANY'],
    [TWENTY_ONE, 'ONE'],
    [HALF, 'FEW'],
    [ONE_AND_HALF, 'FEW'],
] as const;

describe('resolveServingsUnitKey', () => {
    it.each(CASES)('formats %s servings', (count, suffix) => {
        expect(resolveServingsUnitKey(Number(count))).toBe(`RECIPE_DETAIL.SUMMARY.SERVINGS_${suffix}`);
    });
});

describe('resolveIngredientUnitKey', () => {
    it('uses portions for nested recipes and measurement units for products', () => {
        expect(resolveIngredientUnitKey({ nestedRecipeId: 'recipe', amount: HALF })).toBe('RECIPE_DETAIL.SUMMARY.SERVINGS_FEW');
        expect(resolveIngredientUnitKey({ productBaseUnit: 'G', amount: 1 })).toBe('GENERAL.UNITS.G');
    });

    it('leaves text and unselected ingredients without a unit', () => {
        expect(resolveIngredientUnitKey({ amount: 0 })).toBeNull();
        expect(resolveIngredientUnitKey({ nestedRecipeId: null, productBaseUnit: null, amount: 0 })).toBeNull();
        expect(resolveIngredientUnitKey({ nestedRecipeId: '', productBaseUnit: '', amount: 0 })).toBeNull();
    });
});
