import { describe, expect, it } from 'vitest';

import { MeasurementUnit } from '../../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { createRecipeForm } from './recipe-manage-form.mapper';
import { RecipeStepFormManager } from './recipe-step-form.manager';

describe('RecipeStepFormManager', () => {
    it('adds and removes steps while keeping expanded indexes aligned', () => {
        const form = createRecipeForm();
        const manager = createManager(form);

        manager.addStep({ title: 'First', imageUrl: null, description: 'Prep', ingredients: [] });
        manager.addStep({ title: 'Second', imageUrl: null, description: 'Cook', ingredients: [] });
        manager.addStep({ title: 'Third', imageUrl: null, description: 'Serve', ingredients: [] });
        manager.toggleStepExpanded(1);

        manager.removeStep(0);

        expect(form.controls.steps.length).toBe(2);
        expect(form.controls.steps.at(0).controls.title.value).toBe('Second');
        expect(manager.expandedSteps.has(0)).toBe(false);
        expect(manager.expandedSteps.has(1)).toBe(true);
    });

    it('adds, resolves, and removes ingredients inside a step', () => {
        const form = createRecipeForm();
        const manager = createManager(form);
        manager.addStep();

        manager.addIngredientToStep(0);
        const group = manager.getIngredientGroup({ stepIndex: 0, ingredientIndex: 1 });
        group.patchValue({ foodName: 'Apple', amount: 120 });
        manager.removeIngredientFromStep({ stepIndex: 0, ingredientIndex: 0 });

        expect(form.controls.steps.at(0).controls.ingredients.length).toBe(1);
        expect(form.controls.steps.at(0).controls.ingredients.at(0).controls.foodName.value).toBe('Apple');
    });

    it('populates recipe steps using fresh labels from resolver', () => {
        const form = createRecipeForm();
        let labelVersion = 0;
        const manager = new RecipeStepFormManager(form.controls.steps, () => {
            labelVersion += 1;
            return {
                selectIngredient: `Select ${labelVersion}`,
                unknownProduct: `Unknown ${labelVersion}`,
            };
        });

        manager.populateRecipeSteps(createRecipe());

        expect(form.controls.steps.length).toBe(2);
        expect(form.controls.steps.at(0).controls.ingredients.at(0).controls.foodName.value).toBe('Unknown 1');
        expect(form.controls.steps.at(1).controls.ingredients.at(0).controls.foodName.value).toBe('Flour');
    });

    it('creates one empty expanded step when recipe has no steps', () => {
        const form = createRecipeForm();
        const manager = createManager(form);

        manager.populateRecipeSteps({ ...createRecipe(), steps: [] });

        expect(form.controls.steps.length).toBe(1);
        expect(manager.expandedSteps.has(0)).toBe(true);
    });
});

function createManager(form: ReturnType<typeof createRecipeForm>): RecipeStepFormManager {
    return new RecipeStepFormManager(form.controls.steps, () => ({
        selectIngredient: 'Select ingredient',
        unknownProduct: 'Unknown product',
    }));
}

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
