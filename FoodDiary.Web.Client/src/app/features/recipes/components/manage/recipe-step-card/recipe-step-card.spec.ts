import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import type { StepFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeStepValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeStepCardComponent, type RecipeStepCardState } from './recipe-step-card';

describe('RecipeStepCardComponent', () => {
    it('derives first-step state from step index', () => {
        const { component, fixture } = setupComponent();

        expect(component['isFirst']()).toBe(true);

        fixture.componentRef.setInput('stepIndex', 1);
        fixture.detectChanges();

        expect(component['isFirst']()).toBe(false);
    });

    it('trims title when title editing is committed', () => {
        const { component } = setupComponent(
            createRecipeStepValue({ title: '  Cook rice  ', imageUrl: null, description: '', ingredients: [] }),
        );
        const titleChanges: Array<string | null> = [];
        component['stepTitleChange'].subscribe(value => {
            titleChanges.push(value);
        });

        component['toggleStepTitleEdit']();
        component['toggleStepTitleEdit']();

        expect(titleChanges).toContain('Cook rice');
    });

    it('builds ingredient row metadata from ingredient form state', () => {
        const { component } = setupComponent(
            createRecipeStepValue({
                title: null,
                imageUrl: null,
                description: '',
                ingredients: [
                    {
                        food: {
                            id: 'product-1',
                            name: 'Rice',
                            baseUnit: MeasurementUnit.G,
                            baseAmount: 100,
                            defaultPortionAmount: 100,
                            productType: ProductType.Grain,
                            caloriesPerBase: 100,
                            proteinsPerBase: 2,
                            fatsPerBase: 1,
                            carbsPerBase: 20,
                            fiberPerBase: 1,
                            alcoholPerBase: 0,
                            usageCount: 0,
                            visibility: ProductVisibility.Private,
                            createdAt: new Date('2026-01-01T00:00:00Z'),
                            isOwnedByCurrentUser: true,
                            qualityScore: 50,
                            qualityGrade: 'yellow',
                        },
                        productId: 'product-1',
                        foodName: 'Rice',
                        amount: 100,
                        nestedRecipe: null,
                        nestedRecipeId: null,
                        nestedRecipeName: null,
                    },
                ],
            }),
        );

        expect(component['ingredientRows']()[0]).toEqual(
            expect.objectContaining({
                prefixIcon: 'restaurant',
                amountLabel: 'RECIPE_MANAGE.INGREDIENT_AMOUNT (PRODUCT_AMOUNT_UNITS.G)',
            }),
        );
    });
});

function setupComponent(step = createRecipeStepValue()): {
    component: RecipeStepCardComponent;
    fixture: ComponentFixture<RecipeStepCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeStepCardComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeStepCardComponent);
    fixture.componentRef.setInput('step', createStepCardState(step));
    fixture.componentRef.setInput('stepIndex', 0);
    fixture.componentRef.setInput('isExpanded', true);
    fixture.componentRef.setInput('dragDisabled', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function createStepCardState(step: StepFormValues): RecipeStepCardState {
    return {
        title: { value: step.title, error: null },
        imageUrl: { value: step.imageUrl, error: null },
        description: { value: step.description, error: null },
        ingredients: step.ingredients.map(ingredient => ({
            amount: { value: ingredient.amount, error: null },
            food: ingredient.food,
            foodName: { value: ingredient.foodName, error: null },
            nestedRecipeId: ingredient.nestedRecipeId,
        })),
    };
}
