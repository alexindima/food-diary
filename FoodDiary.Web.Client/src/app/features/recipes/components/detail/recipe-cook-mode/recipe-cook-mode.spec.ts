import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

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
        imports: [RecipeCookModeComponent, TranslateModule.forRoot()],
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
