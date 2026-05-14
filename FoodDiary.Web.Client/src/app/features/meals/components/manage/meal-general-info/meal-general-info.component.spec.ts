import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { ConsumptionFormData, ConsumptionItemFormData } from '../meal-manage-lib/meal-manage.types';
import { type MealGeneralFieldErrors, MealGeneralInfoComponent } from './meal-general-info.component';

describe('MealGeneralInfoComponent', () => {
    it('should render with provided form and options', async () => {
        const { fixture } = await setupComponentAsync();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('CONSUMPTION_MANAGE.GENERAL_GROUP_TITLE');
    });
});

type MealGeneralInfoSetup = {
    fixture: ComponentFixture<MealGeneralInfoComponent>;
};

async function setupComponentAsync(): Promise<MealGeneralInfoSetup> {
    await TestBed.configureTestingModule({
        imports: [MealGeneralInfoComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealGeneralInfoComponent);
    fixture.componentRef.setInput('consumptionForm', createConsumptionForm());
    fixture.componentRef.setInput('mealTypeSelectOptions', [{ value: 'BREAKFAST', label: 'Breakfast' }]);
    fixture.componentRef.setInput('generalErrors', createEmptyGeneralErrors());
    fixture.detectChanges();

    return { fixture };
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

function createEmptyGeneralErrors(): MealGeneralFieldErrors {
    return {
        date: null,
        time: null,
        mealType: null,
    };
}
