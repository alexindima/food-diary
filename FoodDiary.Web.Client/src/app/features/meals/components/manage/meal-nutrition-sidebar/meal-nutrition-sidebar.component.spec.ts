import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { NutritionControlNames } from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import type { ConsumptionFormData, ConsumptionItemFormData, MacroBarState, NutritionMode } from '../meal-manage-lib/meal-manage.types';
import { MealNutritionSidebarComponent } from './meal-nutrition-sidebar.component';

describe('MealNutritionSidebarComponent state', () => {
    it('should make nutrition editor readonly in auto mode', async () => {
        const { component } = await setupComponentAsync({ nutritionMode: 'auto' });

        expect(component.isNutritionReadonly()).toBe(true);
        expect(component.showManualNutritionHint()).toBe(false);
    });

    it('should show manual hint in manual mode', async () => {
        const { component } = await setupComponentAsync({ nutritionMode: 'manual' });

        expect(component.isNutritionReadonly()).toBe(false);
        expect(component.showManualNutritionHint()).toBe(true);
    });
});

describe('MealNutritionSidebarComponent actions', () => {
    it('should emit nutrition mode changes and cancel requests', async () => {
        const { component } = await setupComponentAsync();
        const modeHandler = vi.fn();
        const cancelHandler = vi.fn();
        component.nutritionModeChange.subscribe(modeHandler);
        component.cancelRequested.subscribe(cancelHandler);

        component.onNutritionModeChange('manual');
        component.onCancel();

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
        imports: [MealNutritionSidebarComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealNutritionSidebarComponent);
    fixture.componentRef.setInput('consumptionForm', createConsumptionForm());
    fixture.componentRef.setInput('nutritionControlNames', createNutritionControlNames());
    fixture.componentRef.setInput('macroBarState', createMacroBarState());
    fixture.componentRef.setInput('nutritionMode', options.nutritionMode ?? 'auto');
    fixture.componentRef.setInput('nutritionModeOptions', [
        { value: 'auto', label: 'Auto' },
        { value: 'manual', label: 'Manual' },
    ]);
    fixture.componentRef.setInput('nutritionWarning', null);
    fixture.componentRef.setInput('caloriesError', null);
    fixture.componentRef.setInput('macrosError', null);
    fixture.componentRef.setInput('consumption', null);
    fixture.componentRef.setInput('globalError', null);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createConsumptionForm(): FormGroup<ConsumptionFormData> {
    return new FormGroup<ConsumptionFormData>({
        imageUrl: new FormControl(null),
        date: new FormControl('2026-04-05', { nonNullable: true, validators: Validators.required }),
        time: new FormControl('10:30', { nonNullable: true, validators: Validators.required }),
        mealType: new FormControl('BREAKFAST', { nonNullable: true, validators: Validators.required }),
        comment: new FormControl(null),
        items: new FormArray<FormGroup<ConsumptionItemFormData>>([]),
        isNutritionAutoCalculated: new FormControl(true, { nonNullable: true }),
        manualCalories: new FormControl(null),
        manualProteins: new FormControl(null),
        manualFats: new FormControl(null),
        manualCarbs: new FormControl(null),
        manualFiber: new FormControl(null),
        manualAlcohol: new FormControl(null),
        preMealSatietyLevel: new FormControl(null),
        postMealSatietyLevel: new FormControl(null),
    });
}

function createNutritionControlNames(): NutritionControlNames {
    return {
        calories: 'manualCalories',
        proteins: 'manualProteins',
        fats: 'manualFats',
        carbs: 'manualCarbs',
        fiber: 'manualFiber',
        alcohol: 'manualAlcohol',
    };
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
