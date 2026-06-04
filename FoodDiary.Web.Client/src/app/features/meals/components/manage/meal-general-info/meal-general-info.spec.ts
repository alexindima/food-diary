import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { createMealManageFormValue } from '../meal-manage-lib/meal-manage-form.mapper';
import { type MealGeneralFieldErrors, MealGeneralInfoComponent } from './meal-general-info';

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

function createConsumptionForm(): ReturnType<typeof form> {
    return TestBed.runInInjectionContext(() =>
        form(signal({ ...createMealManageFormValue(), date: '2026-04-05', time: '10:30', mealType: 'BREAKFAST' }), path => {
            required(path.date);
            required(path.time);
        }),
    );
}

function createEmptyGeneralErrors(): MealGeneralFieldErrors {
    return {
        date: null,
        time: null,
        mealType: null,
    };
}
