import { describe, expect, it } from 'vitest';

import { entityId } from '../../../shared/models/semantics/entity-id';
import { publicRecipeFromSdk } from './public-recipe-sdk.mapper';

describe('Public recipe SDK ingredients', () => {
    it('preserves unavailable public ingredients without inferring missing nutrition', () => {
        const recipe = publicRecipeFromSdk({
            id: entityId<'recipe'>('recipe'),
            name: 'Public recipe',
            images: [],
            servings: 2,
            missingIngredientCount: 1,
            totalCalories: null,
            totalProteins: 0,
            steps: [
                {
                    stepNumber: 1,
                    instruction: 'Mix',
                    images: [],
                    ingredients: [
                        {
                            name: 'Unavailable ingredient',
                            amount: 0.5,
                            unit: 'PCS',
                            recipeId: 'nested',
                            isAvailable: false,
                        },
                    ],
                },
            ],
        });
        expect(recipe.totalCalories).toBeNull();
        expect(recipe.totalProteins).toBe(0);
        expect(recipe.steps[0].ingredients[0]).toMatchObject({ amount: 0.5, unit: 'PCS', recipeId: 'nested', isAvailable: false });
    });
});
