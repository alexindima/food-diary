import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ImageGalleryEditorComponent } from '../../../../../components/shared/image-gallery-editor/image-gallery-editor';
import { imageSelection } from '../../../../../shared/models/image-upload.data';
import { RecipeVisibility } from '../../../../../shared/models/recipe.data';
import type { RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeFormValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeBasicInfoComponent } from './recipe-basic-info';

describe('RecipeBasicInfoComponent', () => {
    it('keeps the cover and photo order in sync when photos are added, reordered and removed', () => {
        const { fixture, formModel } = setupComponent();
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent))
            .componentInstance as ImageGalleryEditorComponent;
        const first = imageSelection('/first.jpg', 'first');
        const second = imageSelection('/second.jpg', 'second');
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

        gallery.photos.set([imageSelection('/first.jpg', 'first')]);
        fixture.detectChanges();

        expect(recipeForm().dirty()).toBe(true);
    });

    it('marks the provided form dirty when the cover changes', () => {
        const { fixture, formModel, recipeForm } = setupComponent();
        const first = imageSelection('/first.jpg', 'first');
        const second = imageSelection('/second.jpg', 'second');
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
            images: [imageSelection('/first.jpg', 'first')],
            imageUrl: imageSelection('/first.jpg', 'first'),
        }));
        fixture.detectChanges();
        const gallery = fixture.debugElement.query(By.directive(ImageGalleryEditorComponent))
            .componentInstance as ImageGalleryEditorComponent;

        gallery.photos.set([imageSelection('/first.jpg', 'first')]);
        fixture.detectChanges();

        expect(recipeForm().dirty()).toBe(false);
    });
});

describe('RecipeBasicInfoComponent fields', () => {
    it('keeps the selected visibility and summary label in sync when switching language', () => {
        const { fixture, recipeForm } = setupComponent();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { RECIPE_VISIBILITY: { Private: 'Private', Public: 'Public' } });
        translate.setTranslation('ru', { RECIPE_VISIBILITY: { Private: 'Личный', Public: 'Публичный' } });
        translate.use('en');
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const summary = host.querySelector('.recipe-basic-info__additional summary');
        const label = [...host.querySelectorAll('label')].find(element => element.textContent.includes('RECIPE_MANAGE.VISIBILITY'));
        const trigger = label?.control;
        if (!(summary instanceof HTMLElement) || !(trigger instanceof HTMLButtonElement)) {
            throw new Error('Expected the recipe visibility summary and control');
        }
        expect(summary.textContent).toContain('Private');
        summary.click();
        trigger.click();
        fixture.detectChanges();
        const options = document.querySelectorAll<HTMLButtonElement>('[role="listbox"][aria-label="RECIPE_MANAGE.VISIBILITY"] button');
        const publicOption = [...options].find(option => option.textContent.trim() === 'Public');
        if (publicOption === undefined) {
            throw new Error('Expected the public recipe visibility option');
        }
        publicOption.click();
        fixture.detectChanges();
        expect(recipeForm.visibility().value()).toBe(RecipeVisibility.Public);
        expect(summary.textContent).toContain('Public');

        translate.use('ru');
        fixture.detectChanges();
        expect(summary.textContent).toContain('Публичный');
        expect(trigger.textContent).toContain('Публичный');
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
