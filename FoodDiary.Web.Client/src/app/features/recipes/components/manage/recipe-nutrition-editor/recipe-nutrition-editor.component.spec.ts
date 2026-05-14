import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { createRecipeForm } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeNutritionEditorComponent } from './recipe-nutrition-editor.component';

type RecipeNutritionEditorSetup = {
    component: RecipeNutritionEditorComponent;
    fixture: ComponentFixture<RecipeNutritionEditorComponent>;
    form: ReturnType<typeof createRecipeForm>;
};

describe('RecipeNutritionEditorComponent', () => {
    it('should render automatic nutrition state without manual validation errors', async () => {
        const { component } = await setupComponentAsync();

        expect(component.isNutritionReadonly()).toBe(true);
        expect(component.showManualNutritionHint()).toBe(false);
        expect(component.caloriesError()).toBeNull();
        expect(component.macrosError()).toBeNull();
        expect(component.nutritionWarning()).toBeNull();
    });

    it('should expose manual validation errors when manual nutrition is incomplete', async () => {
        const { component, form, fixture } = await setupComponentAsync();

        form.controls.calculateNutritionAutomatically.setValue(false);
        form.controls.manualCalories.setValidators([Validators.required, Validators.min(0)]);
        form.controls.manualCalories.markAsTouched();
        form.controls.manualCalories.updateValueAndValidity();
        form.controls.manualProteins.markAsTouched();
        fixture.detectChanges();

        expect(component.isNutritionReadonly()).toBe(false);
        expect(component.showManualNutritionHint()).toBe(true);
        expect(component.caloriesError()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED');
        expect(component.macrosError()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
    });

    it('should calculate macro state from summary data supplied by the parent', async () => {
        const { component, form, fixture } = await setupComponentAsync();

        form.patchValue({ manualProteins: 10, manualFats: 5, manualCarbs: 35 });
        fixture.detectChanges();

        expect(component.macroBarState()).toEqual({
            isEmpty: false,
            segments: [
                { key: 'proteins', percent: 20 },
                { key: 'fats', percent: 10 },
                { key: 'carbs', percent: 70 },
            ],
        });
    });

    it('should emit nutrition mode changes from the segmented control output', async () => {
        const { component } = await setupComponentAsync();
        let nextMode: string | null = null;
        component.nutritionModeChange.subscribe(value => {
            nextMode = value;
        });

        component.nutritionModeChange.emit('manual');

        expect(nextMode).toBe('manual');
    });
});

async function setupComponentAsync(): Promise<RecipeNutritionEditorSetup> {
    await TestBed.configureTestingModule({
        imports: [RecipeNutritionEditorComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(RecipeNutritionEditorComponent);
    const form = createRecipeForm();
    setRequiredInputs(fixture, form);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
        form,
    };
}

function setRequiredInputs(fixture: ComponentFixture<RecipeNutritionEditorComponent>, form: ReturnType<typeof createRecipeForm>): void {
    fixture.componentRef.setInput('formGroup', form);
    fixture.componentRef.setInput('nutritionScaleMode', 'recipe');
}
