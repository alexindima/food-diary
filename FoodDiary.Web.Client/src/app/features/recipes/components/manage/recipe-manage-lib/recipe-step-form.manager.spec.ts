import { describe, expect, it } from 'vitest';

import { MeasurementUnit } from '../../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import type { StepFormValues } from './recipe-manage.types';
import { RecipeStepFormManager } from './recipe-step-form.manager';

describe('RecipeStepFormManager', () => {
    it('adds and removes steps while keeping expanded indexes aligned', () => {
        const state = createStepState();
        const manager = createManager(state);

        manager.addStep({ title: 'First', imageUrl: null, description: 'Prep', ingredients: [] });
        manager.addStep({ title: 'Second', imageUrl: null, description: 'Cook', ingredients: [] });
        manager.addStep({ title: 'Third', imageUrl: null, description: 'Serve', ingredients: [] });
        manager.toggleStepExpanded(1);

        manager.removeStep(0);

        expect(state.steps.length).toBe(2);
        expect(state.steps[0]?.title).toBe('Second');
        expect(manager.expandedSteps.has(0)).toBe(false);
        expect(manager.expandedSteps.has(1)).toBe(true);
    });

    it('adds, resolves, and removes ingredients inside a step', () => {
        const state = createStepState();
        const manager = createManager(state);
        manager.addStep();

        manager.addIngredientToStep(0);
        manager.patchIngredient({ stepIndex: 0, ingredientIndex: 1 }, { foodName: 'Apple', amount: 120 });
        manager.removeIngredientFromStep({ stepIndex: 0, ingredientIndex: 0 });

        expect(state.steps[0]?.ingredients.length).toBe(1);
        expect(state.steps[0]?.ingredients[0]?.foodName).toBe('Apple');
    });

    it('populates recipe steps using fresh labels from resolver', () => {
        const state = createStepState();
        let labelVersion = 0;
        const manager = new RecipeStepFormManager(
            () => state.steps,
            steps => {
                state.steps = steps;
            },
            () => {
                labelVersion += 1;
                return {
                    selectIngredient: `Select ${labelVersion}`,
                    unknownProduct: `Unknown ${labelVersion}`,
                };
            },
        );

        manager.populateRecipeSteps(createRecipe());

        expect(state.steps.length).toBe(2);
        expect(state.steps[0]?.ingredients[0]?.foodName).toBe('Unknown 1');
        expect(state.steps[1]?.ingredients[0]?.foodName).toBe('Flour');
    });

    it('creates one empty expanded step when recipe has no steps', () => {
        const state = createStepState();
        const manager = createManager(state);

        manager.populateRecipeSteps({ ...createRecipe(), steps: [] });

        expect(state.steps.length).toBe(1);
        expect(manager.expandedSteps.has(0)).toBe(true);
    });
});

function createManager(state: RecipeStepState): RecipeStepFormManager {
    return new RecipeStepFormManager(
        () => state.steps,
        steps => {
            state.steps = steps;
        },
        () => ({
            selectIngredient: 'Select ingredient',
            unknownProduct: 'Unknown product',
        }),
    );
}

function createStepState(): RecipeStepState {
    return { steps: [] };
}

type RecipeStepState = {
    steps: StepFormValues[];
};

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        description: null,
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: null,
        cookTime: null,
        servings: 2,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                title: null,
                instruction: 'Mix',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [
                    {
                        id: 'ingredient-1',
                        amount: 100,
                        productId: 'missing-product',
                    },
                ],
            },
            {
                id: 'step-2',
                stepNumber: 2,
                title: 'Bake',
                instruction: 'Bake',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [
                    {
                        id: 'ingredient-2',
                        amount: 200,
                        productId: 'product-1',
                        productName: 'Flour',
                        productBaseUnit: MeasurementUnit.G,
                        productBaseAmount: 100,
                        productCaloriesPerBase: 364,
                        productProteinsPerBase: 10,
                        productFatsPerBase: 1,
                        productCarbsPerBase: 76,
                        productFiberPerBase: 2,
                        productAlcoholPerBase: 0,
                    },
                ],
            },
        ],
    };
}
