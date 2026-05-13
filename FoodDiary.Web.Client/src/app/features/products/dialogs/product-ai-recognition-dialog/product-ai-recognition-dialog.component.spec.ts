import { HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendLoggerService } from '../../../../services/frontend-logger.service';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { ImageUploadService } from '../../../../shared/api/image-upload.service';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../models/product.data';
import { ProductAiRecognitionDialogComponent } from './product-ai-recognition-dialog.component';

const PRODUCT_CALORIES = 150;
const PRODUCT_PROTEINS = 4;
const PRODUCT_FATS = 2;
const PRODUCT_CARBS = 25;
const PRODUCT_FIBER = 3;
const RECOGNIZED_AMOUNT = 120;
const CONFIDENCE = 0.95;

let fixture: ComponentFixture<ProductAiRecognitionDialogComponent>;
let component: ProductAiRecognitionDialogComponent;
let aiFoodService: {
    analyzeFoodImage: ReturnType<typeof vi.fn>;
    calculateNutrition: ReturnType<typeof vi.fn>;
};
let imageUploadService: { deleteAsset: ReturnType<typeof vi.fn> };
let dialogRef: { close: ReturnType<typeof vi.fn> };
let logger: { warn: ReturnType<typeof vi.fn> };

beforeEach(() => {
    aiFoodService = {
        analyzeFoodImage: vi.fn(),
        calculateNutrition: vi.fn(),
    };
    imageUploadService = {
        deleteAsset: vi.fn().mockReturnValue(of(null)),
    };
    dialogRef = {
        close: vi.fn(),
    };
    logger = {
        warn: vi.fn(),
    };
    aiFoodService.analyzeFoodImage.mockReturnValue(of({ items: [createVisionItem()] }));
    aiFoodService.calculateNutrition.mockReturnValue(of(createNutrition()));

    TestBed.configureTestingModule({
        imports: [ProductAiRecognitionDialogComponent],
        providers: [
            { provide: AiFoodService, useValue: aiFoodService },
            { provide: ImageUploadService, useValue: imageUploadService },
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
        expect(component.descriptionControl.value).toBe(' fresh apple ');
        expect(component.statusKey()).toBeNull();
        expect(component.canApply()).toBe(false);
        expect(component.isAnalyzeDisabled()).toBe(true);
    });

    it('resets previous analysis state when image changes', () => {
        component.results.set([createVisionItem()]);
        component.nutrition.set(createNutrition());
        component.errorKey.set('ERROR');
        component.nutritionErrorKey.set('NUTRITION_ERROR');
        component.hasAnalyzed.set(true);

        component.onImageChanged(createImageSelection());

        expect(component.selection()).toEqual(createImageSelection());
        expect(component.results()).toEqual([]);
        expect(component.nutrition()).toBeNull();
        expect(component.errorKey()).toBeNull();
        expect(component.nutritionErrorKey()).toBeNull();
        expect(component.hasAnalyzed()).toBe(false);
        expect(component.isAnalyzeDisabled()).toBe(false);
    });
});

describe('ProductAiRecognitionDialogComponent analysis', () => {
    it('runs image analysis, calculates nutrition, and applies dialog result', () => {
        component.onImageChanged(createImageSelection());

        component.startAnalysis();
        component.apply();

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({
            imageAssetId: 'asset-1',
            description: 'fresh apple',
        });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({
            items: [{ ...createVisionItem(), unit: 'g' }],
        });
        expect(component.statusKey()).toBe('PRODUCT_AI_DIALOG.STATUS_DONE');
        expect(component.canApply()).toBe(true);
        expect(component.resultForm.controls.name.value).toBe('Apple local');
        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({
                name: 'Apple local',
                image: createImageSelection(),
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
        aiFoodService.analyzeFoodImage.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.Forbidden })));
        component.onImageChanged(createImageSelection());

        component.startAnalysis();

        expect(component.errorKey()).toBe('PRODUCT_AI_DIALOG.ERROR_PREMIUM');
        expect(component.hasAnalyzed()).toBe(true);
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
    });

    it('maps nutrition API errors while keeping recognized items visible', () => {
        aiFoodService.calculateNutrition.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.InternalServerError })));
        component.onImageChanged(createImageSelection());

        component.startAnalysis();

        expect(component.results()).toEqual([createVisionItem()]);
        expect(component.nutrition()).toBeNull();
        expect(component.nutritionErrorKey()).toBe('PRODUCT_AI_DIALOG.NUTRITION_ERROR');
        expect(component.canApply()).toBe(false);
    });
});

describe('ProductAiRecognitionDialogComponent close', () => {
    it('deletes uploaded image asset and closes with null', () => {
        component.onImageChanged(createImageSelection());

        component.close();

        expect(imageUploadService.deleteAsset).toHaveBeenCalledWith('asset-1');
        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });

    it('logs cleanup errors without blocking close', () => {
        imageUploadService.deleteAsset.mockReturnValueOnce(throwError(() => new Error('Delete failed')));
        component.onImageChanged(createImageSelection());

        component.close();

        expect(logger.warn).toHaveBeenCalledWith('Failed to delete AI product image asset', expect.any(Error));
        expect(dialogRef.close).toHaveBeenCalledWith(null);
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
        items: [],
    };
}
