import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ImageGalleryEditorComponent } from '../../../../../components/shared/image-gallery-editor/image-gallery-editor';
import { RecipeVisibility } from '../../../../../shared/models/recipe.data';
import type { RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeFormValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeBasicInfoComponent } from './recipe-basic-info';

describe('RecipeBasicInfoComponent', () => {
    it('keeps the cover and photo order in sync when photos are added, reordered and removed', () => {
        const { fixture, formModel } = setupComponent();
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent))
            .componentInstance as ImageGalleryEditorComponent;
        const first = { url: '/first.jpg', assetId: 'first' };
        const second = { url: '/second.jpg', assetId: 'second' };
        gallery.photos.set([first, second]);
        fixture.detectChanges();
        expect(formModel().imageUrl).toEqual(first);
        gallery.cover.set(second);
        fixture.detectChanges();
        expect(formModel().images).toEqual([second, first]);
        expect(formModel().imageUrl).toEqual(second);
        gallery.cover.set(null);
        expect(formModel().images).toEqual([second, first]);
        gallery.photos.set([]);
        fixture.detectChanges();
        expect(formModel().images).toEqual([]);
        expect(formModel().imageUrl).toBeNull();
    });
});

describe('RecipeBasicInfoComponent photo edit state', () => {
    it('marks the provided form dirty when a photo is added', () => {
        const { fixture, recipeForm } = setupComponent();
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent))
            .componentInstance as ImageGalleryEditorComponent;
        expect(recipeForm().dirty()).toBe(false);

        gallery.photos.set([{ url: '/first.jpg', assetId: 'first' }]);
        fixture.detectChanges();

        expect(recipeForm().dirty()).toBe(true);
    });

    it('marks the provided form dirty when the cover changes', () => {
        const { fixture, formModel, recipeForm } = setupComponent();
        const first = { url: '/first.jpg', assetId: 'first' };
        const second = { url: '/second.jpg', assetId: 'second' };
        formModel.update(value => ({ ...value, images: [first, second], imageUrl: first }));
        fixture.detectChanges();
        expect(recipeForm().dirty()).toBe(false);
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent))
            .componentInstance as ImageGalleryEditorComponent;

        gallery.cover.set(second);
        fixture.detectChanges();

        expect(formModel().imageUrl).toEqual(second);
        expect(recipeForm().dirty()).toBe(true);
    });

    it('keeps photo initialization and equivalent photo selections pristine', () => {
        const { fixture, formModel, recipeForm } = setupComponent();
        formModel.update(value => ({
            ...value,
            images: [{ url: '/first.jpg', assetId: 'first' }],
            imageUrl: { url: '/first.jpg', assetId: 'first' },
        }));
        fixture.detectChanges();
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent))
            .componentInstance as ImageGalleryEditorComponent;

        gallery.photos.set([{ assetId: 'first', url: '/first.jpg' }]);
        fixture.detectChanges();

        expect(recipeForm().dirty()).toBe(false);
    });
});

describe('RecipeBasicInfoComponent fields', () => {
    it('refreshes visibility labels after switching language', () => {
        const { component, fixture } = setupComponent();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { RECIPE_VISIBILITY: { Private: 'Private', Public: 'Public' } });
        translate.setTranslation('ru', { RECIPE_VISIBILITY: { Private: 'Личный', Public: 'Публичный' } });
        translate.use('en');
        fixture.detectChanges();
        expect(component['visibilitySelectOptions']()[0].label).toBe('Private');
        translate.use('ru');
        fixture.detectChanges();
        expect(component['visibilitySelectOptions']()[0].label).toBe('Личный');
    });
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
});

function setupComponent(): {
    component: RecipeBasicInfoComponent;
    fixture: ComponentFixture<RecipeBasicInfoComponent>;
    formModel: ReturnType<typeof signal<RecipeFormValues>>;
    recipeForm: ReturnType<typeof form<RecipeFormValues>>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeBasicInfoComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
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
