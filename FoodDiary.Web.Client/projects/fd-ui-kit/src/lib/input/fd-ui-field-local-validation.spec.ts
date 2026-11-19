import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, FormField, required } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiAutocompleteComponent } from '../autocomplete/fd-ui-autocomplete';
import { FD_VALIDATION_ERRORS } from '../form-error/fd-ui-form-error';
import { FdUiSelectComponent } from '../select/fd-ui-select';
import { FdUiInputComponent } from './fd-ui-input';

@Component({
    imports: [FormField, FdUiInputComponent, FdUiAutocompleteComponent, FdUiSelectComponent],
    template: `
        <fd-ui-input id="recipe-name" label="Recipe name" [formField]="fields.name" />
        <fd-ui-autocomplete id="ingredient-name" label="Ingredient" [formField]="fields.ingredient" />
        <fd-ui-select id="ingredient-unit" label="Unit" [options]="units" [formField]="fields.unit" />
    `,
})
class RecipeFieldsHost {
    public readonly draft = signal({ name: '', ingredient: '', unit: '' });
    public readonly units = [{ value: 'grams', label: 'Grams' }];
    public readonly fields = form(this.draft, path => {
        required(path.name);
        required(path.ingredient);
        required(path.unit);
    });
}

it('announces scoped translated field errors and clears them when the recipe fields become valid', async () => {
    await TestBed.configureTestingModule({
        imports: [RecipeFieldsHost],
        providers: [provideTranslateTesting()],
    })
        .overrideComponent(FdUiInputComponent, { add: { providers: [requiredMessage('RECIPE_VALIDATION.NAME')] } })
        .overrideComponent(FdUiAutocompleteComponent, { add: { providers: [requiredMessage('RECIPE_VALIDATION.INGREDIENT')] } })
        .overrideComponent(FdUiSelectComponent, { add: { providers: [requiredMessage('RECIPE_VALIDATION.UNIT')] } })
        .compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
        RECIPE_VALIDATION: { NAME: 'Enter a recipe name', INGREDIENT: 'Enter an ingredient', UNIT: 'Select a unit' },
    });
    await firstValueFrom(translate.use('en'));
    const fixture = TestBed.createComponent(RecipeFieldsHost);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const name = requireControl(root, '#recipe-name', HTMLInputElement);
    const ingredient = requireControl(root, 'input#ingredient-name', HTMLInputElement);
    const unit = requireControl(root, 'button#ingredient-unit', HTMLButtonElement);
    expect(root.querySelector('[role="alert"]')).toBeNull();

    for (const control of [name, ingredient, unit]) {
        control.focus();
        control.blur();
        fixture.detectChanges();
    }
    expect(Array.from(root.querySelectorAll('[role="alert"]'), alert => alert.textContent.trim())).toEqual([
        'Enter a recipe name',
        'Enter an ingredient',
        'Select a unit',
    ]);

    enterText(name, 'Oatmeal');
    enterText(ingredient, 'Oats');
    unit.click();
    fixture.detectChanges();
    requireControl(document.body, '[role="option"]', HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.draft()).toEqual({ name: 'Oatmeal', ingredient: 'Oats', unit: 'grams' });
    expect(root.querySelector('[role="alert"]')).toBeNull();
});

function requiredMessage(key: string): { provide: typeof FD_VALIDATION_ERRORS; useValue: { required: () => string } } {
    return { provide: FD_VALIDATION_ERRORS, useValue: { required: (): string => key } };
}

function requireControl<T extends HTMLElement>(root: HTMLElement, selector: string, type: abstract new () => T): T {
    const control = root.querySelector(selector);
    if (!(control instanceof type)) {
        throw new Error(`Could not find ${selector}.`);
    }
    return control;
}

function enterText(control: HTMLInputElement, text: string): void {
    control.value = text;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}
