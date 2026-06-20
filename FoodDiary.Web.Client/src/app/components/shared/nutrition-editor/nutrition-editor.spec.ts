import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { NutritionEditorComponent, type NutritionFormModel } from './nutrition-editor';

type NutritionEditorTestContext = {
    fixture: ComponentFixture<NutritionEditorComponent>;
    el: HTMLElement;
};

async function setupNutritionEditorAsync(): Promise<NutritionEditorTestContext> {
    await TestBed.configureTestingModule({
        imports: [NutritionEditorComponent],
        providers: [provideTranslateTesting()],
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

    it('should render field errors below the nutrient cards', async () => {
        const { el, fixture } = await setupNutritionEditorAsync();
        fixture.componentRef.setInput('fieldErrors', {
            proteins: 'Value must be at most 100.',
            fats: 'Value must be at most 100.',
        });

        fixture.detectChanges();

        expect(el.querySelector('.fd-ui-nutrient-input__error')).toBeNull();

        const messagesErrors = el.querySelectorAll('.nutrition-editor-messages__errors');
        expect(messagesErrors.length).toBe(1);
        expect(messagesErrors[0].textContent).toContain('NUTRITION_EDITOR.FIELD_LABELS.PROTEINS');
        expect(messagesErrors[0].textContent).toContain('NUTRITION_EDITOR.FIELD_LABELS.FATS');
        expect(messagesErrors[0].textContent).toContain('Value must be at most 100.');
    });

    it('should use contrast-safe text tokens for nutrient inputs', async () => {
        const { el, fixture } = await setupNutritionEditorAsync();

        fixture.detectChanges();

        const expectedTokens: Record<string, string> = {
            calories: 'var(--fd-color-nutrition-calories-text)',
            proteins: 'var(--fd-color-nutrition-proteins-text)',
            fats: 'var(--fd-color-nutrition-fats-text)',
            carbs: 'var(--fd-color-nutrition-carbs-text)',
            fiber: 'var(--fd-color-nutrition-fiber-text)',
            alcohol: 'var(--fd-color-nutrition-alcohol-text)',
        };

        for (const [nutrient, token] of Object.entries(expectedTokens)) {
            const card = el.querySelector<HTMLElement>(`.nutrition-editor__input--${nutrient} .fd-ui-nutrient-input`);
            expect(card?.style.getPropertyValue('--fd-nutrient-text-color')).toBe(token);
        }
    });

    it('should keep macro bar segments on saturated nutrition colors', async () => {
        const { el, fixture } = await setupNutritionEditorAsync();
        fixture.componentRef.setInput('macroState', {
            isEmpty: false,
            segments: [
                { key: 'proteins', percent: 40 },
                { key: 'fats', percent: 30 },
                { key: 'carbs', percent: 30 },
            ],
        });

        fixture.detectChanges();

        const segments = [...el.querySelectorAll<HTMLElement>('.nutrition-editor__macro-bar-segment')];
        expect(segments.map(segment => segment.style.backgroundColor)).toEqual([
            'var(--fd-color-nutrition-proteins)',
            'var(--fd-color-nutrition-fats)',
            'var(--fd-color-nutrition-carbs)',
        ]);
    });
});
