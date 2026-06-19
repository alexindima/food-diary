import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, max, min } from '@angular/forms/signals';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MANUAL_NUTRITION_MAX_CALORIES, MANUAL_NUTRITION_MAX_NUTRIENT } from '../../../../../shared/lib/nutrition.constants';
import type { NutritionMode, RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeFormValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeNutritionEditorComponent } from './recipe-nutrition-editor';

type RecipeNutritionEditorSetup = {
    component: RecipeNutritionEditorComponent;
    fixture: ComponentFixture<RecipeNutritionEditorComponent>;
    formModel: ReturnType<typeof signal<RecipeFormValues>>;
    recipeForm: ReturnType<typeof form<RecipeFormValues>>;
};

describe('RecipeNutritionEditorComponent', () => {
    it('should render automatic nutrition state without manual validation errors', async () => {
        const { component } = await setupComponentAsync();

        expect(component['isNutritionReadonly']()).toBe(true);
        expect(component['showManualNutritionHint']()).toBe(false);
        expect(component['caloriesError']()).toBeNull();
        expect(component['macrosError']()).toBeNull();
        expect(component['nutritionWarning']()).toBeNull();
    });

    it('should expose manual validation errors when manual nutrition is incomplete', async () => {
        const { component, recipeForm, fixture } = await setupComponentAsync('manual');

        recipeForm.manualCalories().markAsTouched();
        recipeForm.manualProteins().markAsTouched();
        fixture.detectChanges();

        expect(component['isNutritionReadonly']()).toBe(false);
        expect(component['showManualNutritionHint']()).toBe(true);
        expect(component['caloriesError']()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED');
        expect(component['macrosError']()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
    });

    it('should calculate macro state from summary data supplied by the parent', async () => {
        const { component, formModel, fixture } = await setupComponentAsync();

        formModel.update(value => ({
            ...value,
            manualProteins: 10,
            manualFats: 5,
            manualCarbs: 35,
        }));
        fixture.detectChanges();

        expect(component['macroBarState']()).toEqual({
            isEmpty: false,
            segments: [
                { key: 'proteins', percent: 20 },
                { key: 'fats', percent: 10 },
                { key: 'carbs', percent: 70 },
            ],
        });
    });

    it('should expose manual nutrition max field errors', async () => {
        const { component, formModel, recipeForm, fixture } = await setupComponentAsync('manual');

        formModel.update(value => ({
            ...value,
            manualCalories: MANUAL_NUTRITION_MAX_CALORIES + 1,
            manualProteins: MANUAL_NUTRITION_MAX_NUTRIENT + 1,
        }));
        recipeForm.manualCalories().markAsTouched();
        recipeForm.manualProteins().markAsTouched();
        fixture.detectChanges();

        expect(component['maxCalories']).toBe(MANUAL_NUTRITION_MAX_CALORIES);
        expect(component['maxNutrient']).toBe(MANUAL_NUTRITION_MAX_NUTRIENT);
        expect(component['caloriesError']()).toBeNull();
        expect(component['nutritionFieldErrors']().calories).toContain('FORM_ERRORS.INVALID_MAX_AMOUNT');
        expect(component['nutritionFieldErrors']().proteins).toContain('FORM_ERRORS.INVALID_MAX_AMOUNT');
    });

    it('should emit nutrition mode changes from the segmented control output', async () => {
        const { component } = await setupComponentAsync();
        let nextMode: string | null = null;
        component['nutritionModeChange'].subscribe(value => {
            nextMode = value;
        });

        component['nutritionModeChange'].emit('manual');

        expect(nextMode).toBe('manual');
    });
});

async function setupComponentAsync(nutritionMode: NutritionMode = 'auto'): Promise<RecipeNutritionEditorSetup> {
    await TestBed.configureTestingModule({
        imports: [RecipeNutritionEditorComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(RecipeNutritionEditorComponent);
    const formModel = signal(createRecipeFormValue());
    const recipeForm = TestBed.runInInjectionContext(() =>
        form(formModel, path => {
            min(path.manualCalories, 0);
            max(path.manualCalories, MANUAL_NUTRITION_MAX_CALORIES);
            min(path.manualProteins, 0);
            max(path.manualProteins, MANUAL_NUTRITION_MAX_NUTRIENT);
            min(path.manualFats, 0);
            max(path.manualFats, MANUAL_NUTRITION_MAX_NUTRIENT);
            min(path.manualCarbs, 0);
            max(path.manualCarbs, MANUAL_NUTRITION_MAX_NUTRIENT);
            min(path.manualFiber, 0);
            max(path.manualFiber, MANUAL_NUTRITION_MAX_NUTRIENT);
            min(path.manualAlcohol, 0);
            max(path.manualAlcohol, MANUAL_NUTRITION_MAX_NUTRIENT);
        }),
    );
    setRequiredInputs(fixture, recipeForm, nutritionMode);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
        formModel,
        recipeForm,
    };
}

function setRequiredInputs(
    fixture: ComponentFixture<RecipeNutritionEditorComponent>,
    formTree: ReturnType<typeof form<RecipeFormValues>>,
    nutritionMode: NutritionMode,
): void {
    fixture.componentRef.setInput('form', formTree);
    fixture.componentRef.setInput('nutritionMode', nutritionMode);
    fixture.componentRef.setInput('nutritionScaleMode', 'recipe');
}
