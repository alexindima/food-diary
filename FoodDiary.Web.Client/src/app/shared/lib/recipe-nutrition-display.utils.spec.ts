import { describe, expect, it } from 'vitest';

import { recipeNutrientPerServing } from './recipe-nutrition-display.utils';

const RECIPE_CALORIES = 2525;
const SERVING_CALORIES = 505;
const SERVINGS = 5;
const FRACTIONAL_CALORIES = 333.33;
const HALF_FRACTIONAL_CALORIES = 166.665;

describe('recipe nutrition display', () => {
    it('scales the stored recipe total to one serving without rounding away precision', () => {
        expect(recipeNutrientPerServing(RECIPE_CALORIES, SERVINGS)).toBe(SERVING_CALORIES);
        expect(recipeNutrientPerServing(FRACTIONAL_CALORIES, 2)).toBe(HALF_FRACTIONAL_CALORIES);
    });

    it.each([null, undefined])('preserves missing nutrition %s', value => {
        expect(recipeNutrientPerServing(value, SERVINGS)).toBeNull();
    });

    it('preserves a known zero', () => {
        expect(recipeNutrientPerServing(0, SERVINGS)).toBe(0);
    });

    it.each([null, undefined, 0, -1, Number.NaN, Number.POSITIVE_INFINITY])('uses one serving for a legacy invalid count %s', servings => {
        expect(recipeNutrientPerServing(RECIPE_CALORIES, servings)).toBe(RECIPE_CALORIES);
    });
});
