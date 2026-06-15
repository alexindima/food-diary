import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { MacroBarState, NutritionMode } from '../meal-manage-lib/meal-manage.types';
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
});

describe('MealNutritionSidebarComponent actions', () => {
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
    nutritionMode?: NutritionMode;
};

async function setupComponentAsync(
    options: MealNutritionSidebarSetupOptions = {},
): Promise<{ component: MealNutritionSidebarComponent; fixture: ComponentFixture<MealNutritionSidebarComponent> }> {
    await TestBed.configureTestingModule({
        imports: [MealNutritionSidebarComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealNutritionSidebarComponent);
    fixture.componentRef.setInput('consumptionForm', createConsumptionForm());
    fixture.componentRef.setInput('macroBarState', createMacroBarState());
    fixture.componentRef.setInput('nutritionMode', options.nutritionMode ?? 'auto');
    fixture.componentRef.setInput('nutritionWarning', null);
    fixture.componentRef.setInput('caloriesError', null);
    fixture.componentRef.setInput('macrosError', null);
    fixture.componentRef.setInput('isEditMode', false);
    fixture.componentRef.setInput('globalError', null);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createConsumptionForm(): ReturnType<typeof form> {
    return TestBed.runInInjectionContext(() =>
        form(signal({ ...createMealManageFormValue(), date: '2026-04-05', time: '10:30', mealType: 'BREAKFAST' }), path => {
            required(path.date);
            required(path.time);
        }),
    );
}

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
