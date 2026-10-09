import { describe, expect, it } from 'vitest';

import { recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { recipeServingMass } from '../../../../shared/models/semantics/recipe-quantity';
import {
    displayRecipeServings,
    recipeDisplayFromInput,
    recipeDisplayToServings,
    servingMassFromObservation,
    UNKNOWN_SERVING_MASS,
} from './recipe-display-amount';

const FRACTIONAL_SERVINGS = 1.5;
const RECIPE_TOTAL_GRAMS = 100;
const RECIPE_BATCH_SERVINGS = 3;
const GRAMS_PER_SERVING = RECIPE_TOTAL_GRAMS / RECIPE_BATCH_SERVINGS;
const DISPLAY_GRAMS = 50;

describe('recipe display quantity basis', () => {
    it('keeps unknown mass in servings, including fractional amounts', () => {
        const display = displayRecipeServings(recipeServingsFromStored(FRACTIONAL_SERVINGS), UNKNOWN_SERVING_MASS);
        expect(display).toEqual({ unit: 'servings', value: FRACTIONAL_SERVINGS });
        expect(recipeDisplayToServings(display)).toBe(FRACTIONAL_SERVINGS);
    });

    it('preserves precision and carries the mass used for both conversion directions', () => {
        const mass = servingMassFromObservation(GRAMS_PER_SERVING);
        const display = displayRecipeServings(recipeServingsFromStored(FRACTIONAL_SERVINGS), mass);
        expect(display).toEqual({ unit: 'grams', value: DISPLAY_GRAMS, gramsPerServing: GRAMS_PER_SERVING });
        expect(recipeDisplayToServings(display)).toBe(FRACTIONAL_SERVINGS);
        expect(recipeDisplayToServings(recipeDisplayFromInput(DISPLAY_GRAMS, mass))).toBe(FRACTIONAL_SERVINGS);
    });

    it.each([0, -1, Number.NaN, Number.POSITIVE_INFINITY, null])('keeps invalid or missing observed mass %s unknown', value => {
        expect(servingMassFromObservation(value)).toEqual(UNKNOWN_SERVING_MASS);
        if (value !== null) {
            expect(() => recipeServingMass(value)).toThrow(RangeError);
        }
    });

    it.each([0, -1, Number.NaN])('preserves transient raw form amount %s for owning form validation', value => {
        expect(recipeDisplayToServings(recipeDisplayFromInput(value, UNKNOWN_SERVING_MASS))).toBe(value);
        expect(recipeDisplayToServings(recipeDisplayFromInput(value, servingMassFromObservation(GRAMS_PER_SERVING)))).toBe(
            value / GRAMS_PER_SERVING,
        );
    });
});
