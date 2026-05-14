import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { RecipeVisibility } from '../../../models/recipe.data';
import { createRecipeForm } from '../recipe-manage-form.mapper';
import { RecipeBasicInfoComponent } from './recipe-basic-info.component';

describe('RecipeBasicInfoComponent', () => {
    it('builds visibility options inside the component', () => {
        const { component } = setupComponent();

        expect(component.visibilitySelectOptions()).toEqual([
            { value: RecipeVisibility.Private, label: 'RECIPE_VISIBILITY.Private' },
            { value: RecipeVisibility.Public, label: 'RECIPE_VISIBILITY.Public' },
        ]);
    });

    it('resolves field errors from provided form group', () => {
        const { component, form, fixture } = setupComponent();
        form.controls.name.markAsTouched();
        form.controls.name.setValue('');
        form.controls.name.updateValueAndValidity();
        fixture.detectChanges();

        expect(component.fieldErrors().name).toBe('FORM_ERRORS.REQUIRED');
    });
});

function setupComponent(): {
    component: RecipeBasicInfoComponent;
    fixture: ComponentFixture<RecipeBasicInfoComponent>;
    form: ReturnType<typeof createRecipeForm>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeBasicInfoComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(RecipeBasicInfoComponent);
    const form = createRecipeForm();
    fixture.componentRef.setInput('formGroup', form);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture, form };
}
