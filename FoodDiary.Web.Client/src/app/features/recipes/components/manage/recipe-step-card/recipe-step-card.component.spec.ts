import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { MeasurementUnit, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import { createRecipeStepGroup } from '../recipe-manage-form.mapper';
import { RecipeStepCardComponent } from './recipe-step-card.component';

describe('RecipeStepCardComponent', () => {
    it('derives first-step state from step index', () => {
        const { component, fixture } = setupComponent();

        expect(component.isFirst()).toBe(true);

        fixture.componentRef.setInput('stepIndex', 1);
        fixture.detectChanges();

        expect(component.isFirst()).toBe(false);
    });

    it('trims title when title editing is committed', () => {
        const { component, stepGroup } = setupComponent();
        stepGroup.controls.title.setValue('  Cook rice  ');

        component.toggleStepTitleEdit();
        component.toggleStepTitleEdit();

        expect(stepGroup.controls.title.value).toBe('Cook rice');
    });

    it('builds ingredient row metadata from ingredient form state', () => {
        const { component, stepGroup } = setupComponent();
        stepGroup.controls.ingredients.at(0).patchValue({
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
            foodName: 'Rice',
        });

        expect(component.ingredientRows()[0]).toEqual(
            expect.objectContaining({
                prefixIcon: 'restaurant',
                amountLabel: 'RECIPE_MANAGE.INGREDIENT_AMOUNT (PRODUCT_AMOUNT_UNITS.G)',
            }),
        );
    });
});

function setupComponent(): {
    component: RecipeStepCardComponent;
    fixture: ComponentFixture<RecipeStepCardComponent>;
    stepGroup: ReturnType<typeof createRecipeStepGroup>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeStepCardComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(RecipeStepCardComponent);
    const stepGroup = createRecipeStepGroup();
    fixture.componentRef.setInput('stepFormGroup', stepGroup);
    fixture.componentRef.setInput('stepIndex', 0);
    fixture.componentRef.setInput('isExpanded', true);
    fixture.componentRef.setInput('dragDisabled', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture, stepGroup };
}
