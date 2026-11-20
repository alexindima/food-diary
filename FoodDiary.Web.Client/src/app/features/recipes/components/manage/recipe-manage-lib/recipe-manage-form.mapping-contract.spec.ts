import { describe, expect, it } from 'vitest';

import type { RecipeStep, RecipeStepDto } from '../../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../../shared/models/recipe-ingredient';
import { buildRecipeDto, createRecipeFormValue, mapRecipeStepToFormValue } from './recipe-manage-form.mapper';

const mappingLabels = { selectIngredient: 'Select ingredient', unknownProduct: 'Unknown product' };

function saveRestoredStep(step: ReturnType<typeof mapRecipeStepToFormValue>): RecipeStepDto {
    const form = createRecipeFormValue('en');
    return buildRecipeDto({ ...form, name: 'Blueberry pancakes', steps: [step] }, 'recipe', form.servings, value => value ?? 0).steps[0];
}

describe('restoring saved recipe steps for editing', () => {
    it('restores every step photo in order and preserves the gallery when only instructions change', () => {
        const storedStep: RecipeStep = {
            id: 'cook',
            stepNumber: 1,
            title: 'Cook',
            instruction: 'Pour batter',
            imageUrl: '/batter.jpg',
            imageAssetId: 'batter',
            images: [
                { imageUrl: '/batter.jpg', imageAssetId: 'batter' },
                { imageUrl: '/bubbles.jpg', imageAssetId: 'bubbles' },
            ],
            ingredients: [],
        };

        const editedStep = mapRecipeStepToFormValue(storedStep, mappingLabels);

        expect(editedStep.images).toEqual([
            { kind: 'uploaded', url: '/batter.jpg', assetId: 'batter' },
            { kind: 'uploaded', url: '/bubbles.jpg', assetId: 'bubbles' },
        ]);
        expect(saveRestoredStep({ ...editedStep, description: 'Flip when bubbles appear' })).toMatchObject({
            title: 'Cook',
            description: 'Flip when bubbles appear',
            imageUrl: '/batter.jpg',
            imageAssetId: 'batter',
            imageAssetIds: ['batter', 'bubbles'],
        });
    });

    it('omits a legacy ingredient without a usable source and keeps the remaining saved ingredients in order', () => {
        const flourGrams = 120;
        const sauceServings = 2;
        const editedStep = mapRecipeStepToFormValue(
            {
                id: 'mix',
                stepNumber: 1,
                instruction: 'Mix and serve with sauce',
                ingredients: [
                    recipeIngredientFromStored({ id: 'flour', amount: flourGrams, productId: 'product-flour', productName: 'Flour' }),
                    recipeIngredientFromStored({ id: 'legacy', amount: 0, productId: null, nestedRecipeId: null, textName: null }),
                    recipeIngredientFromStored({ id: 'salt', amount: 0, textName: 'Salt', amountText: 'to taste' }),
                    recipeIngredientFromStored({
                        id: 'sauce',
                        amount: sauceServings,
                        nestedRecipeId: 'recipe-sauce',
                        nestedRecipeName: 'Blueberry sauce',
                    }),
                ],
            },
            mappingLabels,
        );

        expect(editedStep.ingredients.map(ingredient => ingredient.textName ?? ingredient.foodName)).toEqual([
            'Flour',
            'Salt',
            'Blueberry sauce',
        ]);
        expect(saveRestoredStep(editedStep).ingredients).toEqual([
            { productId: 'product-flour', amount: flourGrams },
            { textName: 'Salt', amountText: 'to taste', amount: 0 },
            { nestedRecipeId: 'recipe-sauce', amount: sauceServings },
        ]);
    });
});
