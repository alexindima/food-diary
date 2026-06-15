import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { RecipeVisibility } from '../../../models/recipe.data';
import type { RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeFormValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeBasicInfoComponent } from './recipe-basic-info';

describe('RecipeBasicInfoComponent', () => {
    it('builds visibility options inside the component', () => {
        const { component } = setupComponent();

        expect(component['visibilitySelectOptions']()).toEqual([
            { value: RecipeVisibility.Private, label: 'RECIPE_VISIBILITY.Private' },
            { value: RecipeVisibility.Public, label: 'RECIPE_VISIBILITY.Public' },
        ]);
    });

    it('resolves field errors from provided form group', () => {
        const { component, recipeForm, fixture } = setupComponent();
        recipeForm.name().value.set('');
        recipeForm.name().markAsTouched();
        fixture.detectChanges();

        expect(component['fieldErrors']().name).toBe('FORM_ERRORS.REQUIRED');
    });

    it('keeps advanced recipe options collapsed until toggled', () => {
        const { component } = setupComponent();

        expect(component['isAdvancedOpen']()).toBe(false);
        expect(component['advancedToggleIcon']()).toBe('expand_more');

        component['toggleAdvanced']();

        expect(component['isAdvancedOpen']()).toBe(true);
        expect(component['advancedToggleIcon']()).toBe('expand_less');
    });
});

function setupComponent(): {
    component: RecipeBasicInfoComponent;
    fixture: ComponentFixture<RecipeBasicInfoComponent>;
    formModel: ReturnType<typeof signal<RecipeFormValues>>;
    recipeForm: ReturnType<typeof form<RecipeFormValues>>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeBasicInfoComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeBasicInfoComponent);
    const formModel = signal(createRecipeFormValue());
    const recipeForm = TestBed.runInInjectionContext(() =>
        form(formModel, path => {
            required(path.name);
        }),
    );
    fixture.componentRef.setInput('form', recipeForm);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture, formModel, recipeForm };
}
