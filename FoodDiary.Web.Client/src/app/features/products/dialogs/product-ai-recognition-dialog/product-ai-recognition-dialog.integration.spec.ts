import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { FoodRecognitionService } from '../../../../shared/api/food-recognition.service';
import type { FoodRecognitionJobHttpResponse } from '../../../../shared/api/sdk/generated/model/food-recognition-job-http-response';
import { AiFoodFacade } from '../../../../shared/lib/ai-food.facade';
import { ImageUploadFacade } from '../../../../shared/lib/image-upload.facade';
import type { FoodNutritionResponse, FoodVisionItem, FoodVisionResponse } from '../../../../shared/models/ai.data';
import { type FoodRecognitionJob, RECOGNITION_PAGE_SIZE } from '../../../../shared/models/food-recognition.data';
import { type ImageSelection, imageSelection } from '../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../../../shared/models/product.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { publicImageUrl } from '../../../../shared/models/semantics/image-location';
import { ProductAiRecognitionFacade } from '../../lib/product-ai-recognition.facade';
import { ProductAiRecognitionDialogComponent } from './product-ai-recognition-dialog';
import type { ProductAiDialogData, ProductAiRecognitionResult } from './product-ai-recognition-dialog.types';

it('shows label recognition progress and requires confirmation before applying edits and a cover', () => {
    const response = new Subject<FoodVisionResponse>();
    const analyzeFoodImage = vi.fn<ProductAiRecognitionFacade['analyzeFoodImage']>().mockReturnValue(response);
    const calculateNutrition = vi.fn<ProductAiRecognitionFacade['calculateNutrition']>();
    const recognitionCount = vi.fn<ProductAiRecognitionFacade['recognitionCount']>().mockReturnValue(of(0));
    const close = vi.fn<(result: ProductAiRecognitionResult | null) => void>();
    const photo = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const photoOfIngredients = imageSelection('https://images.example.test/ingredients.jpg', 'ingredients-photo');
    const data: ProductAiDialogData = {
        initialDescription: '  Nutrition label  ',
        initialPhotos: [photo, photoOfIngredients],
        hasExistingData: true,
    };
    TestBed.configureTestingModule({
        imports: [ProductAiRecognitionDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: data },
            { provide: FdUiDialogRef, useValue: { close } },
            { provide: ProductAiRecognitionFacade, useValue: { analyzeFoodImage, calculateNutrition, recognitionCount } },
        ],
    });
    const fixture = TestBed.createComponent(ProductAiRecognitionDialogComponent);
    const dialog = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();

    action(dialog, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    fixture.detectChanges();

    expect(analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({
        imageAssetId: photo.assetId,
        additionalImageAssetIds: [photoOfIngredients.assetId],
        description: 'Nutrition label',
        isProductLabel: true,
    });
    expect(dialog.querySelector('.product-ai-dialog__status[role="status"]')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_ANALYZING');
    expect(dialog.querySelector<HTMLButtonElement>('.product-ai-dialog__history-link button')?.disabled).toBe(true);

    response.next({
        items: [],
        productLabel: {
            name: 'Yogurt',
            brand: 'Farm',
            baseAmount: 100,
            baseUnit: 'g',
            calories: 75,
            protein: 5,
            fat: 2,
            carbs: 9,
            fiber: null,
            alcohol: null,
            notes: 'Fiber not listed',
        },
    });
    response.complete();
    fixture.detectChanges();

    expect(calculateNutrition).not.toHaveBeenCalled();
    expect(dialog.querySelector('details')?.textContent).toContain('Fiber not listed');
    expect(dialog.querySelector<HTMLInputElement>('fd-product-ai-recognition-result input')?.value).toBe('Yogurt');
    expect(action(dialog, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(true);
    expect(close).not.toHaveBeenCalled();

    dialog.querySelector<HTMLInputElement>('#product-ai-replace')?.click();
    dialog.querySelector<HTMLInputElement>('#product-ai-cover')?.click();
    fixture.detectChanges();
    action(dialog, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(close).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({
            name: 'Yogurt',
            brand: 'Farm',
            baseUnit: MeasurementUnit.G,
            caloriesPerBase: 75,
            fiberPerBase: null,
            image: photo,
            images: [photo, photoOfIngredients],
        }),
    );
});

it('allows retry after an unavailable provider and an empty reply with the same photo and improved hint', async () => {
    const photo = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo], initialDescription: 'Nutrition label' });
    wizard.analyzeFoodImage
        .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable })))
        .mockReturnValueOnce(of({ items: [] }));

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.ERROR_GENERIC');
    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();

    const hint = requiredElement(wizard.host, 'textarea', HTMLTextAreaElement);
    hint.value = '  Ingredients and nutrition  ';
    hint.dispatchEvent(new Event('input', { bubbles: true }));
    wizard.detectChanges();
    action(wizard.host, 'PRODUCT_AI_DIALOG.REANALYZE').click();
    wizard.detectChanges();

    const status = requiredElement(wizard.host, '.product-ai-dialog__status', HTMLElement);
    expect(status.textContent).toContain('PRODUCT_AI_DIALOG.EMPTY');
    expect(status.textContent).not.toContain('PRODUCT_AI_DIALOG.ERROR_GENERIC');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.REANALYZE').disabled).toBe(false);
    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();
    expect(wizard.analyzeFoodImage).toHaveBeenNthCalledWith(2, {
        imageAssetId: photo.assetId,
        additionalImageAssetIds: [],
        description: 'Ingredients and nutrition',
        isProductLabel: true,
    });
    expect(wizard.calculateNutrition).not.toHaveBeenCalled();
});

it('retains the same photo and hint after a provider nutrition failure and lets the user apply a successful retry', async () => {
    const photo = imageSelection('https://images.example.test/apple.jpg', 'apple-photo');
    const description = 'Apple with peel';
    const wizard = await openWizardAsync({ initialPhotos: [photo], initialDescription: description });
    const item: FoodVisionItem = { nameEn: 'apple', amount: 100, unit: 'g', confidence: 0.9 };
    const retry = new Subject<FoodVisionResponse>();
    wizard.analyzeFoodImage
        .mockReturnValueOnce(
            of({
                items: [item],
                recognition: { id: entityId<'food-recognition'>('failed-estimate'), nutrition: null, errorCode: 'Ai.EstimateUnavailable' },
            }),
        )
        .mockReturnValueOnce(retry);

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();
    expect(requiredElement(wizard.host, '.recognition-photos__preview img', HTMLImageElement).getAttribute('src')).toBe(photo.url);
    expect(requiredElement(wizard.host, 'textarea', HTMLTextAreaElement).value).toBe(description);
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.REANALYZE').disabled).toBe(false);
    expect(wizard.closed).not.toHaveBeenCalled();

    action(wizard.host, 'PRODUCT_AI_DIALOG.REANALYZE').click();
    wizard.detectChanges();
    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_ANALYZING');
    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).not.toContain('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
    expect(wizard.analyzeFoodImage.mock.calls).toEqual([
        [{ imageAssetId: photo.assetId, additionalImageAssetIds: [], description, isProductLabel: true }],
        [{ imageAssetId: photo.assetId, additionalImageAssetIds: [], description, isProductLabel: true }],
    ]);

    const nutrients = { calories: 52, protein: 0.3, fat: 0.2, carbs: 14, fiber: 2, alcohol: 0 };
    retry.next({
        items: [item],
        recognition: {
            id: entityId<'food-recognition'>('successful-estimate'),
            nutrition: { ...nutrients, items: [{ ...nutrients, name: 'Apple', amount: item.amount, unit: item.unit }] },
            errorCode: null,
        },
    });
    retry.complete();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_DONE');
    expect(requiredElement(wizard.host, 'fd-product-ai-recognition-result input', HTMLInputElement).value).toBe('Apple');
    expect(wizard.calculateNutrition).not.toHaveBeenCalled();
    action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ name: 'Apple', caloriesPerBase: 52, images: [photo] }));
});

it('shows the estimating step and keeps the original photo editable when refining an estimate', async () => {
    const photo = imageSelection('https://images.example.test/apple.jpg', 'apple-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo], initialDescription: '   ' });
    const estimate = new Subject<FoodNutritionResponse>();
    const item: FoodVisionItem = { nameEn: 'apple', amount: 100, unit: 'grams', confidence: 0.9 };
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [item] }));
    wizard.calculateNutrition.mockReturnValue(estimate);

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();

    expect(wizard.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({
        imageAssetId: photo.assetId,
        additionalImageAssetIds: [],
        description: null,
        isProductLabel: true,
    });
    expect(wizard.calculateNutrition).toHaveBeenCalledExactlyOnceWith({ items: [{ ...item, unit: 'g' }] });
    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_NUTRITION');
    expect(requiredElement(wizard.host, '.product-ai-dialog__history-link button', HTMLButtonElement).disabled).toBe(true);
    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();

    const estimateNote = 'Estimate assumes the peel is included.';
    estimate.next({ calories: 52, protein: 0.3, fat: 0.2, carbs: 14, fiber: 2, alcohol: 0, items: [], notes: estimateNote });
    estimate.complete();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_DONE');
    expect(wizard.host.querySelector('.product-ai-dialog__item-chip')?.textContent).toBe('Apple');
    expect(requiredElement(wizard.host, 'fd-product-ai-recognition-result input', HTMLInputElement).value).toBe('Apple');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(false);
    const details = requiredElement(wizard.host, 'details.product-ai-dialog__details', HTMLDetailsElement);
    expect(details.open).toBe(false);
    requiredElement(details, 'summary', HTMLElement).click();
    expect(details.open).toBe(true);
    expect(details.querySelector('.product-ai-dialog__note')?.textContent).toBe(estimateNote);
    const displayedNotes = Array.from(wizard.host.querySelectorAll('.product-ai-dialog__note'));
    expect(displayedNotes.filter(note => note.textContent === estimateNote)).toHaveLength(1);

    action(wizard.host, 'PRODUCT_AI_DIALOG.REFINE').click();
    wizard.detectChanges();

    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();
    expect(wizard.host.querySelector('details.product-ai-dialog__details')).toBeNull();
    expect(wizard.host.querySelector<HTMLImageElement>('.recognition-photos__preview img')?.getAttribute('src')).toBe(photo.url);
    const hint = requiredElement(wizard.host, 'textarea', HTMLTextAreaElement);
    expect(hint.disabled).toBe(false);
    expect(hint.value).toBe('   ');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').disabled).toBe(false);
    expect(wizard.analyzeFoodImage).toHaveBeenCalledOnce();
});

it('uses the recognized English name when the localized product name is blank', async () => {
    const photo = imageSelection('https://images.example.test/apple.jpg', 'apple-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    const item: FoodVisionItem = { nameEn: ' apple ', nameLocal: '  ', amount: 100, unit: 'grams', confidence: 0.9 };
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [item] }));
    wizard.calculateNutrition.mockReturnValue(of({ calories: 52, protein: 0.3, fat: 0.2, carbs: 14, fiber: 2, alcohol: 0, items: [] }));

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.product-ai-dialog__item-chip')?.textContent).toBe('Apple');
    expect(requiredElement(wizard.host, 'fd-product-ai-recognition-result input', HTMLInputElement).value).toBe('Apple');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(false);

    action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ name: 'Apple', baseAmount: 100, baseUnit: MeasurementUnit.G, caloriesPerBase: 52 }),
    );
});

it('allows filling in an unreadable label name without estimating nutrients again', async () => {
    const photo = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    wizard.analyzeFoodImage.mockReturnValue(
        of({
            items: [],
            productLabel: {
                name: null,
                brand: 'Farm',
                baseAmount: 100,
                baseUnit: 'g',
                calories: 75,
                protein: 5,
                fat: 2,
                carbs: 9,
                fiber: null,
                alcohol: null,
                notes: null,
            },
        }),
    );

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();
    const name = requiredElement(wizard.host, 'fd-product-ai-recognition-result input', HTMLInputElement);
    expect(name.value).toBe('');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(true);

    name.value = 'Yogurt';
    name.dispatchEvent(new Event('input', { bubbles: true }));
    wizard.detectChanges();
    action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(wizard.calculateNutrition).not.toHaveBeenCalled();
    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ name: 'Yogurt', brand: 'Farm', baseAmount: 100, baseUnit: MeasurementUnit.G, caloriesPerBase: 75 }),
    );
});

it('lets the user name an unidentified estimate and preserves the reviewed zero calories', async () => {
    const photo = imageSelection('https://images.example.test/water.jpg', 'water-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    const portionMilliliters = 500;
    const item: FoodVisionItem = { nameEn: '', nameLocal: ' ', amount: portionMilliliters, unit: 'ml', confidence: 0.5 };
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [item] }));
    wizard.calculateNutrition.mockReturnValue(of({ calories: 0, protein: 0, fat: 0, carbs: 0, fiber: 0, alcohol: 0, items: [] }));

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();

    const name = requiredElement(wizard.host, 'fd-product-ai-recognition-result input', HTMLInputElement);
    expect(name.value).toBe('');
    expect(wizard.host.querySelector('.product-ai-dialog__item-chip')).toBeNull();
    const apply = action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY');
    expect(apply.disabled).toBe(true);
    apply.click();
    expect(wizard.closed).not.toHaveBeenCalled();

    name.value = '  Sparkling water  ';
    name.dispatchEvent(new Event('input', { bubbles: true }));
    wizard.detectChanges();
    expect(apply.disabled).toBe(false);
    apply.click();

    expect(wizard.calculateNutrition).toHaveBeenCalledExactlyOnceWith({ items: [item] });
    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({
            name: 'Sparkling water',
            baseAmount: portionMilliliters,
            baseUnit: MeasurementUnit.ML,
            caloriesPerBase: 0,
        }),
    );
});

it('uses the gram basis for an estimate when the recognition provider supplies no usable unit or amount', async () => {
    const photo = imageSelection('https://images.example.test/apple.jpg', 'apple-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    const item: FoodVisionItem = { nameEn: 'apple', amount: 0, unit: '', confidence: 0.9 };
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [item] }));
    wizard.calculateNutrition.mockReturnValue(of({ calories: 52, protein: 0.3, fat: 0.2, carbs: 14, fiber: 2, alcohol: 0, items: [] }));

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();

    expect(wizard.calculateNutrition).toHaveBeenCalledExactlyOnceWith({ items: [{ ...item, amount: 100, unit: 'g' }] });
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(false);
    action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ name: 'Apple', baseAmount: 100, baseUnit: MeasurementUnit.G, caloriesPerBase: 52 }),
    );
});

it('requires a new image after removing the last photo through its action menu', async () => {
    const photo = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    requiredElement(wizard.host, '.recognition-photos__preview button', HTMLButtonElement).click();
    TestBed.tick();

    const remove = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]')).find(
        button => button.textContent.trim() === 'IMAGE_UPLOAD_FIELD.REMOVE',
    );
    if (remove === undefined) {
        throw new Error('Missing photo removal action');
    }
    remove.click();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.recognition-photos__preview')).toBeNull();
    expect(wizard.host.querySelector('fd-image-upload-field input[type="file"]')).not.toBeNull();
    const analyze = action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE');
    expect(analyze.disabled).toBe(true);
    analyze.click();
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();
});

it('explains that an imported photo on its own needs an additional upload', async () => {
    const imported = imageSelection('https://images.example.test/imported.jpg', null);
    const wizard = await openWizardAsync({ initialPhotos: [imported] });
    const analyze = action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE');

    expect(analyze.disabled).toBe(true);
    expect(analyze.title).toBe('DISABLED_HINTS.IMAGE_REQUIRED');
    expect(wizard.host.querySelector('.recognition-photos__preview img')?.getAttribute('src')).toBe(imported.url);
    analyze.click();
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();
});

it.each([
    { coverKind: 'imported', coverAssetId: null },
    { coverKind: 'uploaded', coverAssetId: 'ingredients-cover' },
])('keeps the gallery and recognition primary when choosing an $coverKind cover', async ({ coverAssetId }) => {
    const packaging = imageSelection('https://images.example.test/packaging.jpg', null);
    const ingredients = imageSelection('https://images.example.test/ingredients.jpg', coverAssetId);
    const label = imageSelection('https://images.example.test/label.jpg', 'uploaded-label');
    const wizard = await openWizardAsync({ initialPhotos: [packaging, label, ingredients] });
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [] }));
    requiredElement(wizard.host, '.recognition-photos__item:nth-child(3) .recognition-photos__preview button', HTMLButtonElement).click();
    TestBed.tick();

    const useAsCover = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]')).find(
        button => button.textContent.trim() === 'PRODUCT_AI_DIALOG.CHOOSE_COVER',
    );
    if (useAsCover === undefined) {
        throw new Error('Missing use as cover action for the imported photo');
    }
    useAsCover.click();
    wizard.detectChanges();

    const previews = wizard.host.querySelectorAll('.recognition-photos__preview img');
    expect(Array.from(previews, preview => preview.getAttribute('src'))).toEqual([ingredients.url, packaging.url, label.url]);

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    expect(wizard.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({
        imageAssetId: label.assetId,
        additionalImageAssetIds: ingredients.assetId === null ? [] : [ingredients.assetId],
        description: null,
        isProductLabel: true,
    });
});

it.each([
    { removedPhoto: 'another photo', removeIndex: 3, keepsLabel: true },
    { removedPhoto: 'the original label', removeIndex: 2, keepsLabel: false },
])('uses the correct recognition primary after choosing a cover and removing $removedPhoto', async ({ removeIndex, keepsLabel }) => {
    const label = imageSelection('https://images.example.test/original-label.jpg', 'original-label');
    const ingredients = imageSelection('https://images.example.test/ingredients.jpg', 'ingredients-cover');
    const extra = imageSelection('https://images.example.test/extra.jpg', 'uploaded-extra');
    const wizard = await openWizardAsync({ initialPhotos: [label, ingredients, extra] });
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [] }));

    chooseRecognitionPhotoAction(wizard, 2, 'PRODUCT_AI_DIALOG.CHOOSE_COVER');
    expect(recognitionPhotoUrls(wizard)).toEqual([ingredients.url, label.url, extra.url]);

    chooseRecognitionPhotoAction(wizard, removeIndex, 'IMAGE_UPLOAD_FIELD.REMOVE');
    const remaining = keepsLabel ? label : extra;
    expect(recognitionPhotoUrls(wizard)).toEqual([ingredients.url, remaining.url]);
    const cover = requiredElement(wizard.host, '.recognition-photos__cover-label:not([hidden])', HTMLSpanElement);
    expect(cover.parentElement?.querySelector('img')?.getAttribute('src')).toBe(ingredients.url);
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    expect(wizard.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({
        imageAssetId: keepsLabel ? label.assetId : ingredients.assetId,
        additionalImageAssetIds: [keepsLabel ? ingredients.assetId : extra.assetId],
        description: null,
        isProductLabel: true,
    });
});

it.each([
    { scenario: 'a different photo is removed', chooseCover: true, removeIndex: 3, applyCover: true },
    { scenario: 'the chosen cover is removed', chooseCover: true, removeIndex: 1, applyCover: false },
    { scenario: 'no cover change was requested', chooseCover: false, removeIndex: 3, applyCover: false },
])('applies the requested Product AI cover when $scenario', async ({ chooseCover, removeIndex, applyCover }) => {
    const existing = imageSelection('https://images.example.test/existing-cover.jpg', 'existing-cover');
    const alternate = imageSelection('https://images.example.test/alternate-cover.jpg', 'alternate-cover');
    const extra = imageSelection('https://images.example.test/extra-label.jpg', 'extra-photo');
    const wizard = await openWizardAsync({ initialPhotos: [existing, alternate, extra], hasExistingData: true });
    wizard.analyzeFoodImage.mockReturnValue(
        of({
            items: [],
            productLabel: {
                name: 'Yogurt',
                brand: null,
                baseAmount: 100,
                baseUnit: 'g',
                calories: 75,
                protein: 5,
                fat: 2,
                carbs: 9,
                fiber: null,
                alcohol: null,
                notes: null,
            },
        }),
    );

    if (chooseCover) {
        chooseRecognitionPhotoAction(wizard, 2, 'PRODUCT_AI_DIALOG.CHOOSE_COVER');
        expect(recognitionPhotoUrls(wizard)).toEqual([alternate.url, existing.url, extra.url]);
    }
    chooseRecognitionPhotoAction(wizard, removeIndex, 'IMAGE_UPLOAD_FIELD.REMOVE');
    const expectedGallery = applyCover ? [alternate, existing] : [existing, removeIndex === 1 ? extra : alternate];
    expect(recognitionPhotoUrls(wizard)).toEqual(expectedGallery.map(photo => photo.url));

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();
    const cover = requiredElement(wizard.host, '#product-ai-cover', HTMLInputElement);
    const apply = action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY');
    expect(apply.disabled).toBe(true);
    requiredElement(wizard.host, '#product-ai-replace', HTMLInputElement).click();
    wizard.detectChanges();
    expect(apply.disabled).toBe(false);
    const checkedInReview = cover.checked;
    apply.click();

    expect(wizard.closed).toHaveBeenCalledTimes(1);
    const result = wizard.closed.mock.calls[0]?.[0];
    expect({ checkedInReview, image: result?.image, images: result?.images }).toEqual({
        checkedInReview: applyCover,
        image: applyCover ? alternate : null,
        images: expectedGallery,
    });
});

it.each([
    { description: 'an uploaded cover photo', assetId: 'label-photo' },
    { description: 'an imported cover photo', assetId: null },
])('waits for the upload before recognizing a label alongside $description', async ({ assetId }) => {
    const photo = imageSelection('https://images.example.test/label.jpg', assetId);
    const additional = imageSelection('https://images.example.test/ingredients.jpg', 'ingredients-photo');
    const upload = new Subject<ImageSelection>();
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    wizard.uploadImage.mockReturnValue(upload);
    wizard.analyzeFoodImage.mockReturnValue(of({ items: [] }));
    const image = document.createElement('img');
    const photoSize = 320;
    Object.defineProperties(image, { naturalWidth: { value: photoSize }, naturalHeight: { value: photoSize } });
    vi.stubGlobal('Image', function createDecodedImage(): HTMLImageElement {
        return image;
    });
    const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:recognition-photo');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    onTestFinished(() => {
        createUrl.mockRestore();
        revokeUrl.mockRestore();
    });
    const picker = requiredElement(wizard.host, 'fd-image-upload-field input[type="file"]', HTMLInputElement);
    const file = new File(['label image'], 'ingredients.png', { type: 'image/png' });
    Object.defineProperty(picker, 'files', { value: [file] });

    picker.dispatchEvent(new Event('change', { bubbles: true }));
    image.dispatchEvent(new Event('load'));
    wizard.detectChanges();

    const analyze = action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE');
    expect(analyze.disabled).toBe(true);
    expect(requiredElement(wizard.host, '.product-ai-dialog__history-link button', HTMLButtonElement).disabled).toBe(true);
    analyze.click();
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();
    await vi.waitFor(() => {
        expect(wizard.uploadImage).toHaveBeenCalledExactlyOnceWith(file);
    });

    upload.next(additional);
    upload.complete();
    await vi.waitFor(() => {
        wizard.detectChanges();
        expect(analyze.disabled).toBe(false);
    });
    const previews = wizard.host.querySelectorAll('.recognition-photos__preview img');
    expect(Array.from(previews, preview => preview.getAttribute('src'))).toEqual([photo.url, additional.url]);

    analyze.click();

    expect(wizard.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({
        imageAssetId: photo.assetId ?? additional.assetId,
        additionalImageAssetIds: photo.assetId === null ? [] : [additional.assetId],
        description: null,
        isProductLabel: true,
    });
});

it.each(['before redraw', 'during upload'] as const)('keeps Apply from omitting a newly selected photo %s', async timing => {
    const primary = imageSelection('https://images.example.test/oat-label.jpg', 'label-photo');
    const additional = imageSelection('https://images.example.test/oat-ingredients.jpg', 'ingredients-photo');
    const upload = new Subject<ImageSelection>();
    const wizard = await openWizardAsync({ initialPhotos: [primary] });
    wizard.uploadImage.mockReturnValue(upload);
    wizard.analyzeFoodImage.mockReturnValue(
        of({
            items: [],
            productLabel: {
                name: 'Oat milk',
                brand: null,
                baseAmount: 100,
                baseUnit: 'ml',
                calories: 45,
                protein: 1,
                fat: 2,
                carbs: 6,
                fiber: null,
                alcohol: null,
                notes: null,
            },
        }),
    );
    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();
    const apply = action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY');
    expect(apply.disabled).toBe(false);

    const image = document.createElement('img');
    const photoSize = 320;
    Object.defineProperties(image, { naturalWidth: { value: photoSize }, naturalHeight: { value: photoSize } });
    vi.stubGlobal('Image', function decodeSelectedPhoto(): HTMLImageElement {
        return image;
    });
    const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:additional-label-photo');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    onTestFinished(() => {
        createUrl.mockRestore();
        revokeUrl.mockRestore();
    });
    const picker = requiredElement(wizard.host, 'fd-image-upload-field input[type="file"]', HTMLInputElement);
    const openPicker = vi.spyOn(picker, 'click');
    requiredElement(wizard.host, '.recognition-photos__empty button', HTMLButtonElement).click();
    expect(openPicker).toHaveBeenCalled();
    const file = new File(['oat ingredients'], 'ingredients.png', { type: 'image/png' });
    Object.defineProperty(picker, 'files', { value: [file] });
    picker.dispatchEvent(new Event('change', { bubbles: true }));
    image.dispatchEvent(new Event('load'));

    if (timing === 'before redraw') {
        expect(apply.disabled).toBe(false);
        apply.click();
        expect(wizard.closed).not.toHaveBeenCalled();
    }
    await vi.waitFor(() => {
        expect(wizard.uploadImage).toHaveBeenCalledExactlyOnceWith(file);
        expect(upload.observed).toBe(true);
    });
    wizard.detectChanges();
    expect(apply.disabled).toBe(true);
    apply.click();
    expect(wizard.closed).not.toHaveBeenCalled();

    upload.next(additional);
    upload.complete();
    await vi.waitFor(() => {
        wizard.detectChanges();
        expect(recognitionPhotoUrls(wizard)).toEqual([primary.url, additional.url]);
        expect(apply.isConnected).toBe(false);
    });
    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();
    expect(wizard.analyzeFoodImage).toHaveBeenNthCalledWith(2, {
        imageAssetId: primary.assetId,
        additionalImageAssetIds: [additional.assetId],
        description: null,
        isProductLabel: true,
    });
    action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').click();
    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ images: [primary, additional] }));
});

it('cancels a two-photo upload after the first photo finishes when the wizard closes', async () => {
    const initialPhoto = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const firstSelection = imageSelection('https://images.example.test/ingredients.jpg', 'ingredients-photo');
    const lateSelection = imageSelection('https://images.example.test/package.jpg', 'package-photo');
    const wizard = await openWizardAsync({ initialPhotos: [initialPhoto] });
    const pending = new Subject<ImageSelection>();
    wizard.uploadImage.mockReturnValueOnce(of(firstSelection)).mockReturnValueOnce(pending);

    const decodedImages: HTMLImageElement[] = [];
    vi.stubGlobal('Image', function createDecodedImage(): HTMLImageElement {
        const image = document.createElement('img');
        const photoSize = 320;
        Object.defineProperties(image, { naturalWidth: { value: photoSize }, naturalHeight: { value: photoSize } });
        decodedImages.push(image);
        return image;
    });
    const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:recognition-photo');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    onTestFinished(() => {
        createUrl.mockRestore();
        revokeUrl.mockRestore();
    });

    const firstFile = new File(['ingredients'], 'ingredients.png', { type: 'image/png' });
    const secondFile = new File(['package'], 'package.png', { type: 'image/png' });
    const picker = requiredElement(wizard.host, 'fd-image-upload-field input[type="file"]', HTMLInputElement);
    expect(picker.multiple).toBe(true);
    Object.defineProperty(picker, 'files', { value: [firstFile, secondFile] });
    picker.dispatchEvent(new Event('change', { bubbles: true }));
    expect(decodedImages).toHaveLength(1);
    decodedImages[0].dispatchEvent(new Event('load'));
    await vi.waitFor(() => {
        expect(wizard.uploadImage).toHaveBeenNthCalledWith(1, firstFile);
        expect(decodedImages).toHaveLength(2);
    });
    decodedImages[1].dispatchEvent(new Event('load'));
    await vi.waitFor(() => {
        expect(wizard.uploadImage).toHaveBeenNthCalledWith(2, secondFile);
        expect(pending.observed).toBe(true);
    });

    wizard.detectChanges();
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').disabled).toBe(true);
    const previews = wizard.host.querySelectorAll('.recognition-photos__preview img');
    expect(Array.from(previews, preview => preview.getAttribute('src'))).toEqual([initialPhoto.url]);

    action(wizard.host, 'PRODUCT_AI_DIALOG.CANCEL').click();

    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(null);
    expect(wizard.host.isConnected).toBe(false);
    expect(pending.observed).toBe(false);
    pending.next(lateSelection);
    pending.complete();
    expect(wizard.closed).toHaveBeenCalledOnce();
    expect(wizard.uploadImage).toHaveBeenCalledTimes(2);
    expect(wizard.deleteImage).not.toHaveBeenCalled();
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();
});

it('closes the rendered wizard during recognition and unsubscribes before a late provider response', async () => {
    const photo = imageSelection('https://images.example.test/label.jpg', 'label-photo');
    const wizard = await openWizardAsync({ initialPhotos: [photo] });
    const pending = new Subject<FoodVisionResponse>();
    wizard.analyzeFoodImage.mockReturnValue(pending);

    action(wizard.host, 'PRODUCT_AI_DIALOG.ANALYZE').click();
    wizard.detectChanges();
    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_ANALYZING');
    expect(pending.observed).toBe(true);

    action(wizard.host, 'COMMON.CLOSE').click();

    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(null);
    expect(wizard.host.isConnected).toBe(false);
    expect(pending.observed).toBe(false);
    pending.next({ items: [{ nameEn: 'apple', amount: 100, unit: 'g', confidence: 0.9 }] });
    pending.complete();
    expect(wizard.calculateNutrition).not.toHaveBeenCalled();
    expect(wizard.deleteImage).not.toHaveBeenCalled();
});

it('resumes a saved product photo with no hint and offers retry when the saved estimate failed', async () => {
    const initialPhoto = imageSelection('https://images.example.test/current.jpg', 'current-photo');
    const timestamp = utcInstant('2025-05-10T08:00:00Z');
    const job: FoodRecognitionJob = {
        id: entityId<'food-recognition'>('past-recognition'),
        imageAssetId: entityId<'image-asset'>('past-photo'),
        imageUrl: publicImageUrl('https://images.example.test/past.jpg'),
        createdOnUtc: timestamp,
        updatedOnUtc: timestamp,
        description: null,
        status: 'Succeeded',
        vision: null,
        nutrition: null,
        errorCode: null,
        nutritionErrorCode: 'Ai.EstimateUnavailable',
    };
    const wizard = await openWizardAsync({ initialPhotos: [initialPhoto], initialDescription: 'Another product' }, [job]);
    wizard.resumeRecognition.mockReturnValue(
        of({
            items: [{ nameEn: 'apple', amount: 100, unit: 'g', confidence: 0.9 }],
            recognition: { id: job.id, nutrition: null, errorCode: job.nutritionErrorCode },
        }),
    );

    requiredElement(wizard.host, '.product-ai-dialog__history-link button', HTMLButtonElement).click();
    wizard.detectChanges();
    const savedPhoto = requiredElement(wizard.host, 'button.history-list__open', HTMLButtonElement);
    expect(savedPhoto.querySelector('img')?.getAttribute('src')).toBe(job.imageUrl);
    savedPhoto.click();
    wizard.detectChanges();

    expect(wizard.listRecognitions).toHaveBeenCalledExactlyOnceWith(1, RECOGNITION_PAGE_SIZE, true);
    expect(wizard.resumeRecognition).toHaveBeenCalledExactlyOnceWith(job.id);
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();
    expect(wizard.calculateNutrition).not.toHaveBeenCalled();
    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();
    expect(wizard.host.querySelector('.recognition-photos__preview img')?.getAttribute('src')).toBe(job.imageUrl);
    expect(requiredElement(wizard.host, 'textarea', HTMLTextAreaElement).value).toBe('');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.REANALYZE').disabled).toBe(false);
});

it('restores the saved primary and additional photos in order and forwards them when applying the result', async () => {
    const timestamp = utcInstant('2025-05-10T08:00:00Z');
    const vision: FoodVisionResponse = {
        items: [],
        productLabel: {
            name: 'Yogurt',
            brand: null,
            baseAmount: 100,
            baseUnit: 'g',
            calories: 75,
            protein: 5,
            fat: 2,
            carbs: 9,
            fiber: null,
            alcohol: null,
            notes: null,
        },
    };
    const job: FoodRecognitionJob = {
        id: entityId<'food-recognition'>('saved-label-recognition'),
        isProductLabel: true,
        imageAssetId: entityId<'image-asset'>('saved-front'),
        imageUrl: publicImageUrl('https://images.example.test/saved-front.jpg'),
        additionalImages: [
            {
                imageAssetId: entityId<'image-asset'>('saved-ingredients'),
                imageUrl: publicImageUrl('https://images.example.test/saved-ingredients.jpg'),
            },
            {
                imageAssetId: entityId<'image-asset'>('saved-nutrition'),
                imageUrl: publicImageUrl('https://images.example.test/saved-nutrition.jpg'),
            },
        ],
        createdOnUtc: timestamp,
        updatedOnUtc: timestamp,
        description: 'Saved yogurt label',
        status: 'Succeeded',
        vision,
        nutrition: null,
        errorCode: null,
        nutritionErrorCode: null,
    };
    const wizard = await openWizardAsync({ initialPhotos: [imageSelection('https://images.example.test/current.jpg', 'current')] }, [job]);
    wizard.resumeRecognition.mockReturnValue(of({ ...vision, recognition: { id: job.id, nutrition: job.nutrition, errorCode: null } }));
    requiredElement(wizard.host, '.product-ai-dialog__history-link button', HTMLButtonElement).click();
    wizard.detectChanges();
    requiredElement(wizard.host, 'button.history-list__open', HTMLButtonElement).click();
    wizard.detectChanges();

    const expected = [
        imageSelection('https://images.example.test/saved-front.jpg', 'saved-front'),
        imageSelection('https://images.example.test/saved-ingredients.jpg', 'saved-ingredients'),
        imageSelection('https://images.example.test/saved-nutrition.jpg', 'saved-nutrition'),
    ];
    const previews = Array.from(wizard.host.querySelectorAll('.recognition-photos__preview img'), image => image.getAttribute('src'));
    expect(previews).toEqual(expected.map(photo => photo.url));
    expect(requiredElement(wizard.host, 'textarea', HTMLTextAreaElement).value).toBe(job.description);
    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_DONE');
    expect(wizard.resumeRecognition).toHaveBeenCalledExactlyOnceWith(job.id);
    expect(wizard.analyzeFoodImage).not.toHaveBeenCalled();
    expect(wizard.calculateNutrition).not.toHaveBeenCalled();

    requiredElement(wizard.host, '#product-ai-cover', HTMLInputElement).click();
    wizard.detectChanges();
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(false);
    action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').click();

    expect(wizard.closed).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ name: 'Yogurt', image: expected[0], images: expected }),
    );
});

const RECOGNITION_URL = `${environment.apiUrls.ai}/food/recognitions`;
const RECOGNITION_POLL_DELAY_MS = 5000;
const TEST_SHA256_BYTES = 32;
const SAVED_LABEL_TIME = '2026-05-10T08:00:00Z';
const SAVED_OAT_LABEL = {
    id: 'c4286db2-ebbd-470d-8922-a2d3729e721c',
    imageAssetId: 'c4286db2-ebbd-470d-8922-a2d3729e721d',
    imageUrl: 'https://images.example.test/oat-label.jpg',
    isProductLabel: true,
    status: 'Succeeded',
    createdOnUtc: SAVED_LABEL_TIME,
    updatedOnUtc: SAVED_LABEL_TIME,
    vision: { items: [], productLabel: { name: 'Oat milk', baseAmount: 100, baseUnit: 'ml', calories: 45, protein: 1 } },
    nutrition: null,
} satisfies FoodRecognitionJobHttpResponse;

it('recovers the saved product label when another history entry is no longer available', async () => {
    const expired = {
        ...SAVED_OAT_LABEL,
        id: 'c4286db2-ebbd-470d-8922-a2d3729e721e',
        imageAssetId: 'c4286db2-ebbd-470d-8922-a2d3729e721f',
        imageUrl: 'https://images.example.test/expired-label.jpg',
        vision: { items: [], productLabel: { name: 'Expired label' } },
    };
    const { http, host, closed } = openHistoryWizardWithHttp();
    TestBed.tick();
    flushProductHistoryPage(http, 1, [expired], 2);
    const openHistory = (): void => {
        requiredElement(host, '.product-ai-dialog__history-link button', HTMLButtonElement).click();
        TestBed.tick();
    };
    openHistory();
    flushProductHistoryPage(http, RECOGNITION_PAGE_SIZE, [expired, SAVED_OAT_LABEL], 2);
    TestBed.tick();
    requiredElement(host, 'button.history-list__open', HTMLButtonElement).click();
    await vi.advanceTimersByTimeAsync(0);
    const expiredUrl = `${RECOGNITION_URL}/${expired.id}`;
    http.expectOne({ method: 'GET', url: expiredUrl }).flush(
        { error: 'Ai.RecognitionNotFound' },
        { status: HttpStatusCode.NotFound, statusText: 'Not Found' },
    );
    TestBed.tick();

    const status = (): HTMLElement => requiredElement(host, '.product-ai-dialog__status', HTMLElement);
    expect(status().textContent).toContain('PRODUCT_AI_DIALOG.ERROR_GENERIC');
    expect(host.querySelector('fd-product-ai-recognition-result')).toBeNull();
    flushProductHistoryPage(http, 1, [SAVED_OAT_LABEL], 1);
    await vi.advanceTimersByTimeAsync(RECOGNITION_POLL_DELAY_MS);
    http.expectNone(expiredUrl);

    openHistory();
    flushProductHistoryPage(http, RECOGNITION_PAGE_SIZE, [SAVED_OAT_LABEL], 1);
    TestBed.tick();
    const available = requiredElement(host, 'button.history-list__open', HTMLButtonElement);
    expect(available.textContent).toContain(SAVED_OAT_LABEL.vision.productLabel.name);
    available.click();
    await vi.advanceTimersByTimeAsync(0);
    http.expectOne({ method: 'GET', url: `${RECOGNITION_URL}/${SAVED_OAT_LABEL.id}` }).flush(SAVED_OAT_LABEL);
    await vi.advanceTimersByTimeAsync(0);
    TestBed.tick();

    expect(status().textContent).toContain('PRODUCT_AI_DIALOG.STATUS_DONE');
    expect(status().textContent).not.toContain('PRODUCT_AI_DIALOG.ERROR_GENERIC');
    expect(host.querySelector('.recognition-photos__preview img')?.getAttribute('src')).toBe(SAVED_OAT_LABEL.imageUrl);
    flushProductHistoryPage(http, 1, [SAVED_OAT_LABEL], 1);
    const apply = action(host, 'PRODUCT_AI_DIALOG.APPLY');
    expect(apply.disabled).toBe(false);
    apply.click();

    expect(closed).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({ name: 'Oat milk', caloriesPerBase: 45, baseUnit: MeasurementUnit.ML }),
    );
    http.expectNone(request => request.method === 'POST');
});

function openHistoryWizardWithHttp(): {
    http: HttpTestingController;
    host: HTMLElement;
    closed: Mock<(result: ProductAiRecognitionResult | null | undefined) => void>;
} {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    const digest = vi.spyOn(crypto.subtle, 'digest').mockResolvedValue(new Uint8Array(TEST_SHA256_BYTES).buffer);
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'setTimeout', 'clearTimeout'] });
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideRouter([]),
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: {
                    getUserId: (): string => 'label-history-user',
                    sessionHubUrl: (): never => {
                        throw new Error('History polling test does not connect to SignalR');
                    },
                },
            },
        ],
    });
    const dialogs = TestBed.inject(FdUiDialogService);
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.useRealTimers();
        vi.unstubAllGlobals();
        digest.mockRestore();
        http.verify();
    });
    const ref = dialogs.open<ProductAiRecognitionDialogComponent, ProductAiDialogData, ProductAiRecognitionResult | null>(
        ProductAiRecognitionDialogComponent,
        { data: {} },
    );
    const component = ref.componentRef;
    if (component === null || !(component.location.nativeElement instanceof HTMLElement)) {
        throw new Error('Recognition dialog did not create its component');
    }
    const closed = vi.fn<(result: ProductAiRecognitionResult | null | undefined) => void>();
    ref.afterClosed().subscribe(closed);
    return { http, host: component.location.nativeElement, closed };
}

function flushProductHistoryPage(
    http: HttpTestingController,
    limit: number,
    jobs: FoodRecognitionJobHttpResponse[],
    totalItems: number,
): void {
    const page = http.expectOne(
        request => request.method === 'GET' && request.url === `${RECOGNITION_URL}/page` && request.params.get('limit') === String(limit),
    );
    expect(page.request.params.get('isProductLabel')).toBe('true');
    page.flush({ data: jobs, page: 1, limit, totalItems, totalPages: 1 });
}

type RecognitionWizard = {
    host: HTMLElement;
    detectChanges: () => void;
    closed: Mock<(result: ProductAiRecognitionResult | null | undefined) => void>;
    analyzeFoodImage: Mock<ProductAiRecognitionFacade['analyzeFoodImage']>;
    calculateNutrition: Mock<ProductAiRecognitionFacade['calculateNutrition']>;
    resumeRecognition: Mock<ProductAiRecognitionFacade['resumeRecognition']>;
    listRecognitions: Mock<AiFoodFacade['listRecognitions']>;
    uploadImage: Mock<ImageUploadFacade['upload']>;
    deleteImage: Mock<ImageUploadFacade['deleteAsset']>;
};

async function openWizardAsync(data: ProductAiDialogData, history: FoodRecognitionJob[] = []): Promise<RecognitionWizard> {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    const analyzeFoodImage = vi.fn<ProductAiRecognitionFacade['analyzeFoodImage']>();
    const calculateNutrition = vi.fn<ProductAiRecognitionFacade['calculateNutrition']>();
    const resumeRecognition = vi.fn<ProductAiRecognitionFacade['resumeRecognition']>();
    const listRecognitionCount = vi
        .fn<FoodRecognitionService['list']>()
        .mockReturnValue(of({ data: history.slice(0, 1), page: 1, limit: 1, totalPages: history.length, totalItems: history.length }));
    const listRecognitions = vi
        .fn<AiFoodFacade['listRecognitions']>()
        .mockReturnValue(of({ data: history, page: 1, limit: RECOGNITION_PAGE_SIZE, totalPages: 1, totalItems: history.length }));
    const uploadImage = vi.fn<ImageUploadFacade['upload']>();
    const deleteImage = vi.fn<ImageUploadFacade['deleteAsset']>().mockReturnValue(of(void 0));

    await TestBed.configureTestingModule({
        imports: [ProductAiRecognitionDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AiFoodService, useValue: { analyzeFoodImage, calculateNutrition } },
            { provide: FoodRecognitionService, useValue: { resume: resumeRecognition, list: listRecognitionCount } },
            { provide: AiFoodFacade, useValue: { listRecognitions } },
            { provide: ImageUploadFacade, useValue: { upload: uploadImage, deleteAsset: deleteImage } },
        ],
    }).compileComponents();
    const ref = TestBed.inject(FdUiDialogService).open<
        ProductAiRecognitionDialogComponent,
        ProductAiDialogData,
        ProductAiRecognitionResult | null
    >(ProductAiRecognitionDialogComponent, { data });
    const component = ref.componentRef;
    if (component === null) {
        throw new Error('Recognition dialog did not create its component');
    }
    const closed = vi.fn<(result: ProductAiRecognitionResult | null | undefined) => void>();
    ref.afterClosed().subscribe(closed);
    const detectChanges = (): void => {
        component.changeDetectorRef.detectChanges();
    };
    detectChanges();
    return {
        host: component.location.nativeElement as HTMLElement,
        detectChanges,
        closed,
        analyzeFoodImage,
        calculateNutrition,
        resumeRecognition,
        listRecognitions,
        uploadImage,
        deleteImage,
    };
}

function requiredElement<T extends HTMLElement>(host: HTMLElement, selector: string, type: new () => T): T {
    const element = host.querySelector(selector);
    if (!(element instanceof type)) {
        throw new Error(`Missing dialog element: ${selector}`);
    }
    return element;
}

function chooseRecognitionPhotoAction(wizard: RecognitionWizard, index: number, label: string): void {
    requiredElement(
        wizard.host,
        `.recognition-photos__item:nth-child(${index}) .recognition-photos__preview button`,
        HTMLButtonElement,
    ).click();
    TestBed.tick();
    const actionButton = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]')).find(
        button => button.textContent.trim() === label,
    );
    if (actionButton === undefined) {
        throw new Error(`Missing recognition photo action: ${label}`);
    }
    actionButton.click();
    wizard.detectChanges();
}

function recognitionPhotoUrls(wizard: RecognitionWizard): string[] {
    return Array.from(wizard.host.querySelectorAll<HTMLImageElement>('.recognition-photos__preview img'), preview => preview.src);
}

function action(dialog: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(dialog.querySelectorAll<HTMLButtonElement>('.product-ai-dialog__footer button')).find(
        item => item.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Missing dialog action: ${label}`);
    }
    return button;
}
