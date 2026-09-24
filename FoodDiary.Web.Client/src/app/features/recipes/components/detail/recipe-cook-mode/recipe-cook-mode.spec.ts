import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { RecipeCookModeComponent } from './recipe-cook-mode';

describe('RecipeCookModeComponent', () => {
    it('steps through recipe instructions', () => {
        const { component, fixture } = setupComponent(createRecipe());

        expect(component['currentStep']()?.instruction).toBe('Mix');
        expect(component['canGoBack']()).toBe(false);
        expect(component['canGoNext']()).toBe(true);

        component['nextStep']();
        fixture.detectChanges();

        expect(component['currentStep']()?.instruction).toBe('Bake');
        expect(component['canGoBack']()).toBe(true);
        expect(component['canGoNext']()).toBe(false);
        expect(component['isDone']()).toBe(true);
    });

    it('builds current-step ingredient views', () => {
        const { component } = setupComponent(createRecipe());

        expect(component['ingredients']()).toEqual([
            {
                name: 'Flour',
                amount: 100,
                unitKey: 'GENERAL.UNITS.G',
            },
        ]);
    });

    it('emits add-to-meal action', () => {
        const { component } = setupComponent(createRecipe());
        const addToMeal = vi.fn();

        component.addToMeal.subscribe(addToMeal);
        component['addRecipeToMeal']();

        expect(addToMeal).toHaveBeenCalledTimes(1);
    });
});

function setupComponent(recipe: Recipe): {
    component: RecipeCookModeComponent;
    fixture: ComponentFixture<RecipeCookModeComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeCookModeComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeCookModeComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Cake',
        description: null,
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: null,
        cookTime: null,
        servings: 4,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        totalCalories: null,
        totalProteins: null,
        totalFats: null,
        totalCarbs: null,
        totalFiber: null,
        totalAlcohol: null,
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
                        productId: 'product-1',
                        productName: 'Flour',
                        productBaseUnit: 'G',
                    },
                ],
            },
            {
                id: 'step-2',
                stepNumber: 2,
                title: null,
                instruction: 'Bake',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [],
            },
        ],
    };
}

describe('RecipeCookModeComponent boundaries', () => {
    it('handles a recipe without instructions without allowing navigation', () => {
        const { component } = setupComponent({ ...createRecipe(), steps: [] });
        component['previousStep']();
        component['nextStep']();
        expect(component['currentStep']()).toBeNull();
        expect(component['ingredients']()).toEqual([]);
        expect(component['progressPercent']()).toBe(0);
        expect(component['isDone']()).toBe(false);
    });

    it('sorts steps without mutating the recipe and does not move beyond boundaries', () => {
        const recipe = createRecipe();
        recipe.steps.reverse();
        const { component } = setupComponent(recipe);
        expect(recipe.steps[0].instruction).toBe('Bake');
        expect(component['currentStep']()?.instruction).toBe('Mix');
        component['previousStep']();
        expect(component['currentStepIndex']()).toBe(0);
        component['nextStep']();
        component['nextStep']();
        expect(component['currentStepIndex']()).toBe(1);
        component['previousStep']();
        expect(component['currentStepIndex']()).toBe(0);
    });

    it('uses nested recipe names and omits unknown measurement units', () => {
        const recipe = createRecipe();
        recipe.steps[0].ingredients = [
            { ...recipe.steps[0].ingredients[0], productName: null, nestedRecipeName: 'Sauce', productBaseUnit: null },
        ];
        const { component } = setupComponent(recipe);
        expect(component['ingredients']()[0]).toMatchObject({ name: 'Sauce', unitKey: '' });
    });
});
