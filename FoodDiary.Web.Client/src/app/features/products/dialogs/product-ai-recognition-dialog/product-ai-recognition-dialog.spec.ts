import { HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendLoggerService } from '../../../../services/frontend-logger.service';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { ProductAiRecognitionFacade } from '../../lib/product-ai-recognition.facade';
import { MeasurementUnit } from '../../models/product.data';
import { ProductAiRecognitionDialogComponent } from './product-ai-recognition-dialog';

const PRODUCT_CALORIES = 150;
const PRODUCT_PROTEINS = 4;
const PRODUCT_FATS = 2;
const PRODUCT_CARBS = 25;
const PRODUCT_FIBER = 3;
const RECOGNIZED_AMOUNT = 120;
const CONFIDENCE = 0.95;

let fixture: ComponentFixture<ProductAiRecognitionDialogComponent>;
let component: ProductAiRecognitionDialogComponent;
let productAiRecognitionFacade: {
    resumeRecognition: ReturnType<typeof vi.fn>;
    analyzeFoodImage: ReturnType<typeof vi.fn>;
    calculateNutrition: ReturnType<typeof vi.fn>;
    deleteAsset: ReturnType<typeof vi.fn>;
};
let dialogRef: { close: ReturnType<typeof vi.fn> };
let logger: { warn: ReturnType<typeof vi.fn> };

beforeEach(() => {
    productAiRecognitionFacade = {
        resumeRecognition: vi.fn(),
        analyzeFoodImage: vi.fn(),
        calculateNutrition: vi.fn(),
        deleteAsset: vi.fn().mockReturnValue(of(null)),
    };
    dialogRef = {
        close: vi.fn(),
    };
    logger = {
        warn: vi.fn(),
    };
    productAiRecognitionFacade.analyzeFoodImage.mockReturnValue(of({ items: [createVisionItem()] }));
    productAiRecognitionFacade.calculateNutrition.mockReturnValue(of(createNutrition()));

    TestBed.configureTestingModule({
        imports: [ProductAiRecognitionDialogComponent],
        providers: [
            { provide: ProductAiRecognitionFacade, useValue: productAiRecognitionFacade },
            { provide: FrontendLoggerService, useValue: logger },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: { initialDescription: ' fresh apple ' } },
        ],
    });
    TestBed.overrideComponent(ProductAiRecognitionDialogComponent, {
        set: { template: '' },
    });

    fixture = TestBed.createComponent(ProductAiRecognitionDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('ProductAiRecognitionDialogComponent state', () => {
    it('initializes description and disables analysis until image is selected', () => {
        expect(component['descriptionModel']().description).toBe(' fresh apple ');
        expect(component['statusKey']()).toBeNull();
        expect(component['canApply']()).toBe(false);
        expect(component['isAnalyzeDisabled']()).toBe(true);
    });

    it('resets previous analysis state when image changes', () => {
        component['results'].set([createVisionItem()]);
        component['nutrition'].set(createNutrition());
        component['errorKey'].set('ERROR');
        component['nutritionErrorKey'].set('NUTRITION_ERROR');
        component['hasAnalyzed'].set(true);

        component['onImageChanged'](createImageSelection());

        expect(component['selection']()).toEqual(createImageSelection());
        expect(component['results']()).toEqual([]);
        expect(component['nutrition']()).toBeNull();
        expect(component['errorKey']()).toBeNull();
        expect(component['nutritionErrorKey']()).toBeNull();
        expect(component['hasAnalyzed']()).toBe(false);
        expect(component['isAnalyzeDisabled']()).toBe(false);
    });
});

describe('ProductAiRecognitionDialogComponent analysis', () => {
    it('runs image analysis, calculates nutrition, and applies dialog result', () => {
        component['onImageChanged'](createImageSelection());

        component['startAnalysis']();
        component['apply']();

        expect(productAiRecognitionFacade.analyzeFoodImage).toHaveBeenCalledWith({
            imageAssetId: 'asset-1',
            isProductLabel: true,
            additionalImageAssetIds: [],
            description: 'fresh apple',
        });
        expect(productAiRecognitionFacade.calculateNutrition).toHaveBeenCalledWith({
            items: [{ ...createVisionItem(), unit: 'g' }],
        });
        expect(component['statusKey']()).toBe('PRODUCT_AI_DIALOG.STATUS_DONE');
        expect(component['canApply']()).toBe(true);
        expect(component['resultFormModel']().name).toBe('Apple local');
        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({
                name: 'Apple local',
                image: null,
                description: null,
                baseUnit: MeasurementUnit.G,
                caloriesPerBase: PRODUCT_CALORIES,
                proteinsPerBase: PRODUCT_PROTEINS,
                fatsPerBase: PRODUCT_FATS,
                carbsPerBase: PRODUCT_CARBS,
                fiberPerBase: PRODUCT_FIBER,
            }),
        );
    });

    it('maps recognition API errors and skips nutrition calculation', () => {
        productAiRecognitionFacade.analyzeFoodImage.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.Forbidden })));
        component['onImageChanged'](createImageSelection());

        component['startAnalysis']();

        expect(component['errorKey']()).toBe('PRODUCT_AI_DIALOG.ERROR_PREMIUM');
        expect(component['hasAnalyzed']()).toBe(true);
        expect(productAiRecognitionFacade.calculateNutrition).not.toHaveBeenCalled();
    });

    it('maps nutrition API errors while keeping recognized items visible', () => {
        productAiRecognitionFacade.calculateNutrition.mockReturnValueOnce(
            throwError(() => ({ status: HttpStatusCode.InternalServerError })),
        );
        component['onImageChanged'](createImageSelection());

        component['startAnalysis']();

        expect(component['results']()).toEqual([createVisionItem()]);
        expect(component['nutrition']()).toBeNull();
        expect(component['nutritionErrorKey']()).toBe('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
        expect(component['canApply']()).toBe(false);
    });
});

describe('ProductAiRecognitionDialogComponent close', () => {
    it('preserves the uploaded image for background processing and closes', () => {
        component['onImageChanged'](createImageSelection());

        component['close']();

        expect(productAiRecognitionFacade.deleteAsset).not.toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });

    it('recovers saved nutrition without repeating recognition or nutrition calls', () => {
        productAiRecognitionFacade.resumeRecognition.mockReturnValue(
            of({
                items: [createVisionItem()],
                recognition: { id: 'job-1', nutrition: createNutrition(), errorCode: null },
            }),
        );
        component['onResumeRecognition']({
            id: 'job-1',
            imageAssetId: 'asset-1',
            imageUrl: 'https://example.test/image.jpg',
            description: 'apple',
            status: 'Succeeded',
            createdOnUtc: '2026-09-11T00:00:00Z',
            updatedOnUtc: '2026-09-11T00:00:00Z',
            vision: null,
            nutrition: null,
            errorCode: null,
            nutritionErrorCode: null,
        });
        expect(productAiRecognitionFacade.resumeRecognition).toHaveBeenCalledWith('job-1');
        expect(productAiRecognitionFacade.analyzeFoodImage).not.toHaveBeenCalled();
        expect(productAiRecognitionFacade.calculateNutrition).not.toHaveBeenCalled();
        expect(component['nutrition']()).toEqual(createNutrition());
        expect(component['selection']()?.assetId).toBe('asset-1');
    });
});

function createImageSelection(): ImageSelection {
    return {
        assetId: 'asset-1',
        url: 'https://example.test/image.jpg',
    };
}

function createVisionItem(): FoodVisionItem {
    return {
        nameEn: 'apple',
        nameLocal: 'apple local',
        amount: RECOGNIZED_AMOUNT,
        unit: 'grams',
        confidence: CONFIDENCE,
        centerX: 0.42,
        centerY: 0.58,
        locationConfidence: 0.88,
    };
}

function createNutrition(): FoodNutritionResponse {
    return {
        calories: PRODUCT_CALORIES,
        protein: PRODUCT_PROTEINS,
        fat: PRODUCT_FATS,
        carbs: PRODUCT_CARBS,
        fiber: PRODUCT_FIBER,
        alcohol: 0,
        items: [
            {
                name: 'Apple',
                amount: RECOGNIZED_AMOUNT,
                unit: 'g',
                calories: PRODUCT_CALORIES,
                protein: PRODUCT_PROTEINS,
                fat: PRODUCT_FATS,
                carbs: PRODUCT_CARBS,
                fiber: PRODUCT_FIBER,
                alcohol: 0,
            },
        ],
    };
}

describe('Product recognition review safeguards', () => {
    it('only applies the photo when explicitly selected as cover', () => {
        component['onImageChanged'](createImageSelection());
        component['startAnalysis']();
        component['useAsCover'].set(true);
        component['apply']();
        expect(dialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ image: createImageSelection(), description: null }));
    });

    it('does not call successful empty recognition done or allow applying it', () => {
        productAiRecognitionFacade.analyzeFoodImage.mockReturnValue(of({ items: [] }));
        component['onImageChanged'](createImageSelection());
        component['startAnalysis']();
        expect(component['isEmpty']()).toBe(true);
        expect(component['statusKey']()).toBeNull();
        component['apply']();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('preserves the photo and hint after failure and supports a retry', () => {
        productAiRecognitionFacade.analyzeFoodImage.mockReturnValueOnce(throwError(() => new Error('offline')));
        component['onImageChanged'](createImageSelection());
        component['startAnalysis']();
        expect(component['statusKey']()).toBeNull();
        expect(component['isEmpty']()).toBe(false);
        expect(component['selection']()).toEqual(createImageSelection());
        expect(component['descriptionModel']().description).toBe(' fresh apple ');
        component['reanalyze']();
        expect(component['canApply']()).toBe(true);
        expect(component['errorKey']()).toBeNull();
    });

    it('prevents duplicate requests while recognition runs', () => {
        const pending = new Subject<{ items: FoodVisionItem[] }>();
        productAiRecognitionFacade.analyzeFoodImage.mockReturnValue(pending);
        component['onImageChanged'](createImageSelection());
        component['startAnalysis']();
        component['startAnalysis']();
        expect(productAiRecognitionFacade.analyzeFoodImage).toHaveBeenCalledTimes(1);
        expect(component['isBusy']()).toBe(true);
        expect(component['canApply']()).toBe(false);
        fixture.destroy();
        expect(pending.observed).toBe(false);
        expect(productAiRecognitionFacade.deleteAsset).not.toHaveBeenCalled();
    });

    it.each([NaN, Infinity, -1])('does not apply an invalid nutrient: %s', value => {
        component['onImageChanged'](createImageSelection());
        component['startAnalysis']();
        component['resultFormModel'].update(model => ({ ...model, fiberPerBase: value }));
        component['apply']();
        expect(component['canApply']()).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('requires explicit acceptance before replacing existing product data', () => {
        fixture.destroy();
        const data = TestBed.inject(FD_UI_DIALOG_DATA) as { hasExistingData?: boolean };
        data.hasExistingData = true;
        fixture = TestBed.createComponent(ProductAiRecognitionDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        component['onImageChanged'](createImageSelection());
        component['startAnalysis']();
        component['apply']();
        expect(dialogRef.close).not.toHaveBeenCalled();
        component['replacementAccepted'].set(true);
        component['apply']();
        expect(dialogRef.close).toHaveBeenCalledTimes(1);
        component['reanalyze']();
        expect(component['replacementAccepted']()).toBe(false);
    });
});

describe('product label recognition', () => {
    it('uses all photos in one label request and never calculates nutrients from names', () => {
        const label = {
            name: 'Yogurt',
            brand: 'Dairy',
            baseAmount: 100,
            baseUnit: 'g',
            calories: 63.5,
            protein: 5,
            fat: 2,
            carbs: 6,
            fiber: null,
            alcohol: null,
            notes: 'Fiber not on label',
        };
        productAiRecognitionFacade.analyzeFoodImage.mockReturnValue(of({ items: [], productLabel: label }));
        const second = { assetId: 'asset-2', url: 'https://example.test/label.jpg' };
        component['onPhotosChanged']([createImageSelection(), second]);
        component['onCoverChanged'](second);
        component['startAnalysis']();
        expect(productAiRecognitionFacade.analyzeFoodImage).toHaveBeenCalledWith({
            imageAssetId: 'asset-1',
            additionalImageAssetIds: ['asset-2'],
            isProductLabel: true,
            description: 'fresh apple',
        });
        expect(productAiRecognitionFacade.calculateNutrition).not.toHaveBeenCalled();
        expect(component['hasResult']()).toBe(true);
        expect(component['isEmpty']()).toBe(false);
        component['apply']();
        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({
                name: 'Yogurt',
                brand: 'Dairy',
                image: second,
                fiberPerBase: null,
                alcoholPerBase: null,
                caloriesPerBase: 63.5,
            }),
        );
    });
    it('does not invent an unknown label basis or allow applying it', () => {
        productAiRecognitionFacade.analyzeFoodImage.mockReturnValue(
            of({
                items: [],
                productLabel: {
                    name: 'Yogurt',
                    brand: null,
                    baseAmount: null,
                    baseUnit: null,
                    calories: 63,
                    protein: null,
                    fat: null,
                    carbs: null,
                    fiber: null,
                    alcohol: null,
                    notes: 'Unreadable label',
                },
            }),
        );
        component['onPhotosChanged']([createImageSelection()]);
        component['startAnalysis']();
        expect(component['resultFormModel']().portionAmount).toBeNull();
        expect(component['resultFormModel']().baseUnit).toBeNull();
        expect(component['canApply']()).toBe(false);
    });
    it('does not submit while an additional photo is uploading', () => {
        component['onPhotosChanged']([createImageSelection()]);
        component['photoUploading'].set(true);
        component['startAnalysis']();
        expect(productAiRecognitionFacade.analyzeFoodImage).not.toHaveBeenCalled();
    });
});

it('prefills existing photos without starting recognition or mutating the form photo array', () => {
    const photos = [createImageSelection(), { assetId: 'second', url: '/second.jpg' }];
    Object.assign(TestBed.inject(FD_UI_DIALOG_DATA), { initialPhotos: photos });
    const prefilled = TestBed.createComponent(ProductAiRecognitionDialogComponent).componentInstance;
    expect(prefilled['photos']()).toEqual(photos);
    expect(prefilled['photos']()).not.toBe(photos);
    expect(prefilled['isAnalyzeDisabled']()).toBe(false);
    expect(productAiRecognitionFacade.analyzeFoodImage).not.toHaveBeenCalled();
});
