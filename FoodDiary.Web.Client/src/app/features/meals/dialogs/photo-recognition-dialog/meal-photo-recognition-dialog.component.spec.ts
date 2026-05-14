import { HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AiFoodService } from '../../../../shared/api/ai-food.service';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { MealAiSessionManageDto } from '../../models/meal.data';
import { MealPhotoRecognitionDialogComponent } from './meal-photo-recognition-dialog.component';

const SOURCE_AMOUNT = 100;
const EDITED_AMOUNT = 150;
const BASE_CALORIES = 100;
const BASE_PROTEIN = 10;
const BASE_FAT = 5;
const BASE_CARBS = 20;
const BASE_FIBER = 2;
const EXPECTED_EDITED_CALORIES = 150;

const visionItem: FoodVisionItem = {
    nameEn: 'Apple',
    nameLocal: 'Яблоко',
    amount: SOURCE_AMOUNT,
    unit: 'g',
    confidence: 0.9,
};

const nutrition: FoodNutritionResponse = {
    calories: BASE_CALORIES,
    protein: BASE_PROTEIN,
    fat: BASE_FAT,
    carbs: BASE_CARBS,
    fiber: BASE_FIBER,
    alcohol: 0,
    notes: 'notes',
    items: [
        {
            name: 'Apple',
            amount: SOURCE_AMOUNT,
            unit: 'g',
            calories: BASE_CALORIES,
            protein: BASE_PROTEIN,
            fat: BASE_FAT,
            carbs: BASE_CARBS,
            fiber: BASE_FIBER,
            alcohol: 0,
        },
    ],
};

let aiFoodService: {
    analyzeFoodImage: ReturnType<typeof vi.fn>;
    calculateNutrition: ReturnType<typeof vi.fn>;
};
let dialogRef: { close: ReturnType<typeof vi.fn> };

describe('MealPhotoRecognitionDialogComponent analysis', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('should analyze selected image and calculate nutrition', async () => {
        const { component } = await setupComponentAsync();

        component.onImageChanged({ assetId: 'asset-1', url: 'https://example.test/photo.jpg' });

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset-1' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: [visionItem] });
        expect(component.results()).toEqual([visionItem]);
        expect(component.nutrition()).toEqual(nutrition);
        expect(component.statusKey()).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.STATUS_DONE');
    });

    it('should set premium error when image analysis is forbidden', async () => {
        aiFoodService.analyzeFoodImage.mockReturnValue(throwError(() => ({ status: HttpStatusCode.Forbidden })));
        const { component } = await setupComponentAsync();

        component.onImageChanged({ assetId: 'asset-1', url: null });

        expect(component.errorKey()).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
        expect(component.hasAnalyzed()).toBe(true);
        expect(component.isLoading()).toBe(false);
    });

    it('should set nutrition quota error when nutrition calculation is rate limited', async () => {
        aiFoodService.calculateNutrition.mockReturnValue(throwError(() => ({ status: HttpStatusCode.TooManyRequests })));
        const { component } = await setupComponentAsync();

        component.onImageChanged({ assetId: 'asset-1', url: null });

        expect(component.nutritionErrorKey()).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
        expect(component.nutrition()).toBeNull();
        expect(component.isNutritionLoading()).toBe(false);
    });
});

describe('MealPhotoRecognitionDialogComponent editing', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('should recalculate nutrition locally when only amount changes', async () => {
        const { component } = await setupComponentAsync();
        component.results.set([visionItem]);
        component.nutrition.set(nutrition);
        component.startEditing();
        aiFoodService.calculateNutrition.mockClear();

        component.updateEditItem(0, 'amount', String(EDITED_AMOUNT));
        component.applyEditing();

        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(component.nutrition()?.calories).toBe(EXPECTED_EDITED_CALORIES);
        expect(component.nutrition()?.items[0].amount).toBe(EDITED_AMOUNT);
    });

    it('should request nutrition again when edited name changes', async () => {
        const { component } = await setupComponentAsync();
        component.results.set([visionItem]);
        component.nutrition.set(nutrition);
        component.startEditing();
        aiFoodService.calculateNutrition.mockClear();

        component.updateEditItem(0, 'name', 'Pear');
        component.applyEditing();

        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({
            items: [{ nameEn: 'Pear', nameLocal: 'Pear', amount: SOURCE_AMOUNT, unit: 'g', confidence: 1 }],
        });
    });

    it('should reorder, remove and add edit items', async () => {
        const { component } = await setupComponentAsync();
        component.results.set([visionItem, { ...visionItem, nameEn: 'Banana', nameLocal: null }]);
        component.startEditing();

        component.onEditItemDrop({ previousIndex: 0, currentIndex: 1 });
        expect(component.editItems()[1].nameEn).toBe('Apple');

        component.removeEditItem(1);
        expect(component.editItems()).toHaveLength(1);

        component.addEditItem();
        expect(component.editItems()).toHaveLength(2);
        expect(component.editItems()[1].unit).toBe('g');
    });
});

describe('MealPhotoRecognitionDialogComponent session payload', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('should restore initial session in edit mode and close with mapped payload', async () => {
        const session = createSession();
        const { component } = await setupComponentAsync({
            initialSelection: { assetId: 'asset-1', url: 'https://example.test/photo.jpg' },
            initialSession: session,
            mode: 'edit',
        });

        expect(component.isEditMode()).toBe(true);
        expect(component.results()).toEqual([
            {
                nameEn: 'Apple',
                nameLocal: 'Яблоко',
                amount: SOURCE_AMOUNT,
                unit: 'g',
                confidence: 1,
            },
        ]);
        expect(component.nutrition()?.calories).toBe(BASE_CALORIES);

        component.addToMeal();

        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({
                imageAssetId: 'asset-1',
                imageUrl: 'https://example.test/photo.jpg',
                notes: null,
                items: [
                    expect.objectContaining({
                        nameEn: 'Apple',
                        nameLocal: 'Яблоко',
                        calories: BASE_CALORIES,
                        proteins: BASE_PROTEIN,
                    }),
                ],
            }),
        );
    });

    it('should close with null on cancel', async () => {
        const { component } = await setupComponentAsync();

        component.close();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

async function setupComponentAsync(
    data: Record<string, unknown> | null = null,
): Promise<{ component: MealPhotoRecognitionDialogComponent; fixture: ComponentFixture<MealPhotoRecognitionDialogComponent> }> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoRecognitionDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: AiFoodService, useValue: aiFoodService },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealPhotoRecognitionDialogComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createAiFoodServiceMock(): { analyzeFoodImage: ReturnType<typeof vi.fn>; calculateNutrition: ReturnType<typeof vi.fn> } {
    return {
        analyzeFoodImage: vi.fn().mockReturnValue(of({ items: [visionItem] })),
        calculateNutrition: vi.fn().mockReturnValue(of(nutrition)),
    };
}

function createSession(): MealAiSessionManageDto {
    return {
        imageAssetId: 'asset-1',
        imageUrl: 'https://example.test/photo.jpg',
        recognizedAtUtc: '2026-05-14T10:00:00Z',
        items: [
            {
                nameEn: 'Apple',
                nameLocal: 'Яблоко',
                amount: SOURCE_AMOUNT,
                unit: 'g',
                calories: BASE_CALORIES,
                proteins: BASE_PROTEIN,
                fats: BASE_FAT,
                carbs: BASE_CARBS,
                fiber: BASE_FIBER,
                alcohol: 0,
            },
        ],
    };
}
