import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { NutritionEditorComponent, type NutritionFormModel } from './nutrition-editor';

type NutritionEditorTestContext = {
    fixture: ComponentFixture<NutritionEditorComponent>;
    el: HTMLElement;
};

async function setupNutritionEditorAsync(): Promise<NutritionEditorTestContext> {
    await TestBed.configureTestingModule({
        imports: [NutritionEditorComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(NutritionEditorComponent);
    const formModel = signal<NutritionFormModel>({
        calories: 0,
        proteins: 0,
        fats: 0,
        carbs: 0,
        fiber: 0,
        alcohol: 0,
    });
    const nutritionForm = TestBed.runInInjectionContext(() => form(formModel));
    fixture.componentRef.setInput('form', {
        calories: nutritionForm.calories,
        proteins: nutritionForm.proteins,
        fats: nutritionForm.fats,
        carbs: nutritionForm.carbs,
        fiber: nutritionForm.fiber,
        alcohol: nutritionForm.alcohol,
    });
    fixture.componentRef.setInput('macroState', {
        isEmpty: true,
        segments: [],
    });

    return {
        fixture,
        el: fixture.nativeElement as HTMLElement,
    };
}

describe('NutritionEditorComponent', () => {
    it('should not render error containers when errors are absent', async () => {
        const { el, fixture } = await setupNutritionEditorAsync();

        fixture.detectChanges();

        expect(el.querySelectorAll('.nutrition-editor__errors').length).toBe(0);
    });

    it('should not render error containers for blank error text', async () => {
        const { el, fixture } = await setupNutritionEditorAsync();
        fixture.componentRef.setInput('caloriesError', '   ');
        fixture.componentRef.setInput('macrosError', '');

        fixture.detectChanges();

        expect(el.querySelectorAll('.nutrition-editor__errors').length).toBe(0);
    });

    it('should render error containers when error text is provided', async () => {
        const { el, fixture } = await setupNutritionEditorAsync();
        fixture.componentRef.setInput('caloriesError', 'Calories are required.');
        fixture.componentRef.setInput('macrosError', 'Macros do not match.');

        fixture.detectChanges();

        const caloriesErrors = el.querySelectorAll('.nutrition-editor__errors');
        expect(caloriesErrors.length).toBe(1);
        expect(caloriesErrors[0].textContent).toContain('Calories are required.');

        const macrosErrors = el.querySelectorAll('.nutrition-editor-messages__errors');
        expect(macrosErrors.length).toBe(1);
        expect(macrosErrors[0].textContent).toContain('Macros do not match.');
    });
});
