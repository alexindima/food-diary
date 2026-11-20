import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { FoodRecognitionService } from '../../../../shared/api/food-recognition.service';
import { ImageUploadService } from '../../../../shared/api/image-upload.service';
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

    estimate.next({ calories: 52, protein: 0.3, fat: 0.2, carbs: 14, fiber: 2, alcohol: 0, items: [] });
    estimate.complete();
    wizard.detectChanges();

    expect(wizard.host.querySelector('.product-ai-dialog__status')?.textContent).toContain('PRODUCT_AI_DIALOG.STATUS_DONE');
    expect(wizard.host.querySelector('.product-ai-dialog__item-chip')?.textContent).toBe('Apple');
    expect(requiredElement(wizard.host, 'fd-product-ai-recognition-result input', HTMLInputElement).value).toBe('Apple');
    expect(action(wizard.host, 'PRODUCT_AI_DIALOG.APPLY').disabled).toBe(false);

    action(wizard.host, 'PRODUCT_AI_DIALOG.REFINE').click();
    wizard.detectChanges();

    expect(wizard.host.querySelector('fd-product-ai-recognition-result')).toBeNull();
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

type RecognitionWizard = {
    host: HTMLElement;
    detectChanges: () => void;
    closed: Mock<(result: ProductAiRecognitionResult | null | undefined) => void>;
    analyzeFoodImage: Mock<ProductAiRecognitionFacade['analyzeFoodImage']>;
    calculateNutrition: Mock<ProductAiRecognitionFacade['calculateNutrition']>;
    resumeRecognition: Mock<ProductAiRecognitionFacade['resumeRecognition']>;
    listRecognitions: Mock<AiFoodFacade['listRecognitions']>;
    uploadImage: Mock<ImageUploadFacade['upload']>;
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

    await TestBed.configureTestingModule({
        imports: [ProductAiRecognitionDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AiFoodService, useValue: { analyzeFoodImage, calculateNutrition } },
            { provide: FoodRecognitionService, useValue: { resume: resumeRecognition, list: listRecognitionCount } },
            { provide: ImageUploadService, useValue: {} },
            { provide: AiFoodFacade, useValue: { listRecognitions } },
            { provide: ImageUploadFacade, useValue: { upload: uploadImage } },
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
    };
}

function requiredElement<T extends HTMLElement>(host: HTMLElement, selector: string, type: new () => T): T {
    const element = host.querySelector(selector);
    if (!(element instanceof type)) {
        throw new Error(`Missing dialog element: ${selector}`);
    }
    return element;
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
