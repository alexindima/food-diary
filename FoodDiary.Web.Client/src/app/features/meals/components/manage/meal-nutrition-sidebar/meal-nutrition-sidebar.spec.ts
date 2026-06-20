import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, max, required } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MANUAL_NUTRITION_MAX_CALORIES, MANUAL_NUTRITION_MAX_NUTRIENT } from '../../../../../shared/lib/nutrition.constants';
import type { ConsumptionFormValues, MacroBarState, NutritionMode } from '../meal-manage-lib/meal-manage.types';
import { createMealManageFormValue } from '../meal-manage-lib/meal-manage-form.mapper';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar';

describe('MealNutritionSidebarComponent state', () => {
    it('should make nutrition editor readonly in auto mode', async () => {
        const { component } = await setupComponentAsync({ nutritionMode: 'auto' });

        expect(component['isNutritionReadonly']()).toBe(true);
        expect(component['showManualNutritionHint']()).toBe(false);
    });

    it('should show manual hint in manual mode', async () => {
        const { component } = await setupComponentAsync({ nutritionMode: 'manual' });

        expect(component['isNutritionReadonly']()).toBe(false);
        expect(component['showManualNutritionHint']()).toBe(true);
    });

    it('should expose manual nutrition max field errors in manual mode', async () => {
        const formModel = signal<ConsumptionFormValues>({
            ...createMealManageFormValue(),
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            manualCalories: MANUAL_NUTRITION_MAX_CALORIES + 1,
            manualFats: MANUAL_NUTRITION_MAX_NUTRIENT + 1,
        });
        const { component, consumptionForm, fixture } = await setupComponentAsync({ nutritionMode: 'manual', formModel });
        consumptionForm.manualCalories().markAsTouched();
        consumptionForm.manualFats().markAsTouched();
        fixture.detectChanges();

        expect(component['maxCalories']).toBe(MANUAL_NUTRITION_MAX_CALORIES);
        expect(component['maxNutrient']).toBe(MANUAL_NUTRITION_MAX_NUTRIENT);
        expect(component['nutritionFieldErrors']().calories).toContain('FORM_ERRORS.INVALID_MAX_AMOUNT');
        expect(component['nutritionFieldErrors']().fats).toContain('FORM_ERRORS.INVALID_MAX_AMOUNT');
    });
});

describe('MealNutritionSidebarComponent actions', () => {
    it('should disable submit action when requested', async () => {
        const { fixture } = await setupComponentAsync({ submitDisabled: true });
        const submitButton = (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]');

        expect(submitButton?.hasAttribute('disabled')).toBe(true);
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('CONSUMPTION_MANAGE.SUBMIT_DISABLED_ITEMS_HINT');
    });

    it('should prioritize global error over disabled submit hint', async () => {
        const { fixture } = await setupComponentAsync({ globalError: 'FORM_ERRORS.NON_EMPTY_ARRAY', submitDisabled: true });
        const text = (fixture.nativeElement as HTMLElement).textContent;

        expect(text).toContain('FORM_ERRORS.NON_EMPTY_ARRAY');
        expect(text).not.toContain('CONSUMPTION_MANAGE.SUBMIT_DISABLED_ITEMS_HINT');
    });

    it('should emit nutrition mode changes and cancel requests', async () => {
        const { component } = await setupComponentAsync();
        const modeHandler = vi.fn();
        const cancelHandler = vi.fn();
        component['nutritionModeChange'].subscribe(modeHandler);
        component['cancelRequested'].subscribe(cancelHandler);

        component['onNutritionModeChange']('manual');
        component['onCancel']();

        expect(modeHandler).toHaveBeenCalledWith('manual');
        expect(cancelHandler).toHaveBeenCalled();
    });
});

type MealNutritionSidebarSetupOptions = {
    formModel?: ReturnType<typeof signal<ConsumptionFormValues>>;
    globalError?: string | null;
    nutritionMode?: NutritionMode;
    submitDisabled?: boolean;
};

async function setupComponentAsync(options: MealNutritionSidebarSetupOptions = {}): Promise<{
    component: MealNutritionSidebarComponent;
    consumptionForm: ConsumptionSignalForm;
    fixture: ComponentFixture<MealNutritionSidebarComponent>;
}> {
    await TestBed.configureTestingModule({
        imports: [MealNutritionSidebarComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealNutritionSidebarComponent);
    const consumptionForm = createConsumptionForm(options.formModel);
    fixture.componentRef.setInput('consumptionForm', consumptionForm);
    fixture.componentRef.setInput('macroBarState', createMacroBarState());
    fixture.componentRef.setInput('nutritionMode', options.nutritionMode ?? 'auto');
    fixture.componentRef.setInput('nutritionWarning', null);
    fixture.componentRef.setInput('caloriesError', null);
    fixture.componentRef.setInput('macrosError', null);
    fixture.componentRef.setInput('isEditMode', false);
    fixture.componentRef.setInput('isSubmitting', false);
    fixture.componentRef.setInput('submitDisabled', options.submitDisabled ?? false);
    fixture.componentRef.setInput('globalError', options.globalError ?? null);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        consumptionForm,
        fixture,
    };
}

function createConsumptionForm(
    formModel: ReturnType<typeof signal<ConsumptionFormValues>> = signal<ConsumptionFormValues>({
        ...createMealManageFormValue(),
        date: '2026-04-05',
        time: '10:30',
        mealType: 'BREAKFAST',
    }),
): ConsumptionSignalForm {
    return TestBed.runInInjectionContext(() =>
        form(formModel, path => {
            required(path.date);
            required(path.time);
            max(path.manualCalories, MANUAL_NUTRITION_MAX_CALORIES);
            max(path.manualProteins, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualFats, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualCarbs, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualFiber, MANUAL_NUTRITION_MAX_NUTRIENT);
            max(path.manualAlcohol, MANUAL_NUTRITION_MAX_NUTRIENT);
        }),
    );
}

type ConsumptionSignalForm = ReturnType<typeof form<ConsumptionFormValues>>;

function createMacroBarState(): MacroBarState {
    return {
        isEmpty: false,
        segments: [
            { key: 'proteins', percent: 30 },
            { key: 'fats', percent: 20 },
            { key: 'carbs', percent: 50 },
        ],
    };
}
