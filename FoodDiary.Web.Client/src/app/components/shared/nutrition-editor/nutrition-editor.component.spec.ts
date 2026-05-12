import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { type NutritionControlNames, NutritionEditorComponent } from './nutrition-editor.component';

const CONTROL_NAMES: NutritionControlNames = {
    calories: 'calories',
    proteins: 'proteins',
    fats: 'fats',
    carbs: 'carbs',
    fiber: 'fiber',
    alcohol: 'alcohol',
};

type NutritionEditorTestContext = {
    fixture: ComponentFixture<NutritionEditorComponent>;
    el: HTMLElement;
};

async function setupNutritionEditorAsync(): Promise<NutritionEditorTestContext> {
    await TestBed.configureTestingModule({
        imports: [NutritionEditorComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(NutritionEditorComponent);
    fixture.componentRef.setInput(
        'formGroup',
        new FormGroup({
            calories: new FormControl(0),
            proteins: new FormControl(0),
            fats: new FormControl(0),
            carbs: new FormControl(0),
            fiber: new FormControl(0),
            alcohol: new FormControl(0),
        }),
    );
    fixture.componentRef.setInput('controlNames', CONTROL_NAMES);
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

        const errors = el.querySelectorAll('.nutrition-editor__errors');
        expect(errors.length).toBe(2);
        expect(errors[0].textContent).toContain('Calories are required.');
        expect(errors[1].textContent).toContain('Macros do not match.');
    });
});
