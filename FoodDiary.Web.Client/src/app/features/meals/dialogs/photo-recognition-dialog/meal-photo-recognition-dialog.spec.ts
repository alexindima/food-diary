import { HttpStatusCode } from '@angular/common/http';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AiFoodFacade } from '../../../../shared/lib/ai-food.facade';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import type { MealAiSessionManageDto } from '../../models/meal.data';
import { MealPhotoRecognitionDialogComponent } from './meal-photo-recognition-dialog';

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
    centerX: 0.42,
    centerY: 0.58,
    locationConfidence: 0.92,
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
    resumeRecognition: ReturnType<typeof vi.fn>;
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

        component['onImageChanged']({ assetId: 'asset-1', url: 'https://example.test/photo.jpg' });

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset-1' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: [visionItem] });
        expect(component['results']()).toEqual([visionItem]);
        expect(component['nutrition']()).toEqual(nutrition);
        expect(component['statusKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_DONE');
    });

    it('should build photo annotations and let the user hide them', async () => {
        const { component } = await setupComponentAsync();

        component['onImageChanged']({ assetId: 'asset-1', url: 'https://example.test/photo.jpg' });

        expect(component['annotations']()).toEqual([
            expect.objectContaining({
                name: visionItem.nameLocal,
                centerX: 42,
                centerY: 58,
                calories: BASE_CALORIES,
            }),
        ]);

        component['toggleAnnotations']();

        expect(component['annotationsVisible']()).toBe(false);
    });

    it('should set premium error when image analysis is forbidden', async () => {
        aiFoodService.analyzeFoodImage.mockReturnValue(throwError(() => ({ status: HttpStatusCode.Forbidden })));
        const { component } = await setupComponentAsync();

        component['onImageChanged']({ assetId: 'asset-1', url: null });

        expect(component['errorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
        expect(component['hasAnalyzed']()).toBe(true);
        expect(component['isLoading']()).toBe(false);
    });

    it('should set nutrition quota error when nutrition calculation is rate limited', async () => {
        aiFoodService.calculateNutrition.mockReturnValue(throwError(() => ({ status: HttpStatusCode.TooManyRequests })));
        const { component } = await setupComponentAsync();

        component['onImageChanged']({ assetId: 'asset-1', url: null });

        expect(component['nutritionErrorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA');
        expect(component['nutrition']()).toBeNull();
        expect(component['isNutritionLoading']()).toBe(false);
    });
});

describe('MealPhotoRecognitionDialogComponent editing', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('should recalculate nutrition locally when only amount changes', async () => {
        const { component } = await setupComponentAsync();
        component['results'].set([visionItem]);
        component['nutrition'].set(nutrition);
        component['startEditing']();
        aiFoodService.calculateNutrition.mockClear();

        component['updateEditItem'](0, 'amount', String(EDITED_AMOUNT));
        component['applyEditing']();

        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(component['nutrition']()?.calories).toBe(EXPECTED_EDITED_CALORIES);
        expect(component['nutrition']()?.items[0].amount).toBe(EDITED_AMOUNT);
    });

    it('should request nutrition again when edited name changes', async () => {
        const { component } = await setupComponentAsync();
        component['results'].set([visionItem]);
        component['nutrition'].set(nutrition);
        component['startEditing']();
        aiFoodService.calculateNutrition.mockClear();

        component['updateEditItem'](0, 'name', 'Pear');
        component['applyEditing']();

        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({
            items: [{ nameEn: 'Pear', nameLocal: 'Pear', amount: SOURCE_AMOUNT, unit: 'g', confidence: 0.9 }],
        });
    });

    it('should reorder, remove and add edit items', async () => {
        const { component } = await setupComponentAsync();
        component['results'].set([visionItem, { ...visionItem, nameEn: 'Banana', nameLocal: null }]);
        component['startEditing']();

        component['onEditItemDrop']({ previousIndex: 0, currentIndex: 1 });
        expect(component['editItems']()[1].nameEn).toBe('Apple');

        component['removeEditItem'](1);
        expect(component['editItems']()).toHaveLength(1);

        component['addEditItem']();
        expect(component['editItems']()).toHaveLength(2);
        expect(component['editItems']()[1].unit).toBe('g');
    });

    it('should exclude rejected edit items from recalculated nutrition', async () => {
        const { component } = await setupComponentAsync();
        component['results'].set([visionItem]);
        component['nutrition'].set(nutrition);
        component['startEditing']();

        component['updateEditItem'](0, 'resolution', 'Rejected');
        component['applyEditing']();

        expect(component['nutrition']()?.calories).toBe(0);
        expect(component['nutrition']()?.items).toEqual([]);
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

        expect(component['isEditMode']()).toBe(true);
        expect(component['results']()).toEqual([
            {
                nameEn: 'Apple',
                nameLocal: 'Яблоко',
                amount: SOURCE_AMOUNT,
                unit: 'g',
                confidence: 1,
            },
        ]);
        expect(component['nutrition']()?.calories).toBe(BASE_CALORIES);

        component['addToMeal']();

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
                        confidence: 1,
                        resolution: 'Accepted',
                    }),
                ],
            }),
        );
    });

    it('should close with null on cancel', async () => {
        const { component } = await setupComponentAsync();

        component['close']();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

async function setupComponentAsync(
    data: Record<string, unknown> | null = null,
): Promise<{ component: MealPhotoRecognitionDialogComponent; fixture: ComponentFixture<MealPhotoRecognitionDialogComponent> }> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoRecognitionDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AiFoodFacade, useValue: aiFoodService },
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

function createAiFoodServiceMock(): typeof aiFoodService {
    return {
        resumeRecognition: vi.fn().mockReturnValue(of({ items: [visionItem], recognition: { nutrition, errorCode: null } })),
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
                confidence: 1,
                resolution: 'Accepted',
            },
        ],
    };
}

describe('Meal photo asynchronous requests', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });
    it('cancels obsolete analysis and nutrition requests when changing the image', async () => {
        const analysis = new Subject<{ items: FoodVisionItem[] }>();
        const calculation = new Subject<FoodNutritionResponse>();
        aiFoodService.analyzeFoodImage.mockReturnValueOnce(analysis);
        aiFoodService.calculateNutrition.mockReturnValueOnce(calculation);
        const { component, fixture } = await setupComponentAsync();
        component['onImageChanged']({ assetId: 'first', url: null });
        expect(component['isLoading']()).toBe(true);
        analysis.next({ items: [visionItem] });
        expect(component['isNutritionLoading']()).toBe(true);
        component['onImageChanged']({ assetId: 'second', url: null });
        expect(analysis.observed).toBe(false);
        expect(calculation.observed).toBe(false);
        calculation.next({ ...nutrition, calories: 999 });
        expect(component['nutrition']()?.calories).toBe(BASE_CALORIES);
        fixture.destroy();
    });
    it('cancels in-flight analysis on destruction', async () => {
        const pending = new Subject<{ items: FoodVisionItem[] }>();
        aiFoodService.analyzeFoodImage.mockReturnValue(pending);
        const { component, fixture } = await setupComponentAsync();
        component['onImageChanged']({ assetId: 'first', url: null });
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
    it('does not reuse review items from the previous image after clearing selection', async () => {
        const { component } = await setupComponentAsync();
        component['onImageChanged']({ assetId: 'first', url: null });
        expect(component['reviewItems']().length).toBe(1);
        component['onImageChanged'](null);
        expect(component['reviewItems']()).toEqual([]);
        component['addToMeal']();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
    it.each([HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError])(
        'clears loading and supports retry after HTTP %s',
        async status => {
            aiFoodService.analyzeFoodImage.mockReturnValueOnce(throwError(() => ({ status })));
            const { component } = await setupComponentAsync();
            component['onImageChanged']({ assetId: 'first', url: null });
            expect(component['isLoading']()).toBe(false);
            expect(component['errorKey']()).toContain(status === HttpStatusCode.TooManyRequests ? 'ERROR_QUOTA' : 'ERROR_GENERIC');
            component['onReanalyze']();
            expect(component['errorKey']()).toBeNull();
            expect(component['nutrition']()).toEqual(nutrition);
        },
    );
    it('handles an empty recognition without calling nutrition', async () => {
        aiFoodService.analyzeFoodImage.mockReturnValue(of({ items: [] }));
        const { component } = await setupComponentAsync();
        component['onImageChanged']({ assetId: 'empty', url: null });
        expect(component['hasAnalyzed']()).toBe(true);
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        component['onImageChanged'](null);
        component['onReanalyze']();
        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledTimes(1);
    });
});

describe('Meal photo recovered jobs and editor controls', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });
    it.each([null, 'Ai.QuotaExceeded', 'Other'])('resumes a job with nutrition outcome %s without a duplicate request', async errorCode => {
        aiFoodService.resumeRecognition.mockReturnValue(of({ items: [visionItem], recognition: { nutrition, errorCode } }));
        const { component } = await setupComponentAsync();
        component['onResumeRecognition']({
            id: 'job-1',
            imageAssetId: 'asset-1',
            imageUrl: '/image.jpg',
            status: 'Succeeded',
            description: null,
            createdOnUtc: '2026-01-01T12:00:00Z',
            updatedOnUtc: '2026-01-01T12:01:00Z',
            vision: null,
            nutrition,
            errorCode: null,
            nutritionErrorCode: errorCode,
        });
        expect(aiFoodService.resumeRecognition).toHaveBeenCalledWith('job-1');
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(component['nutrition']()).toEqual(nutrition);
        expect(component['nutritionErrorKey']()).toBe(
            errorCode === null
                ? null
                : errorCode === 'Ai.QuotaExceeded'
                  ? 'MEAL_MANAGE.PHOTO_AI_DIALOG.ERROR_QUOTA'
                  : 'MEAL_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR',
        );
    });
    it('applies editor events to the saved session and scales nutrition locally', async () => {
        const { component } = await setupComponentAsync({ initialSession: createSession(), mode: 'edit' });
        component['applyEditAction']();
        component['updateEditItemFromView']({ index: 0, field: 'amount', value: String(EDITED_AMOUNT) });
        component['applyEditAction']();
        component['addToMeal']();
        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({
                items: [expect.objectContaining({ amount: EDITED_AMOUNT, calories: EXPECTED_EDITED_CALORIES })],
            }),
        );
    });
    it('allows retry after a nutrition network failure', async () => {
        aiFoodService.calculateNutrition.mockReturnValueOnce(throwError(() => new Error('offline')));
        const { component } = await setupComponentAsync();
        component['onImageChanged']({ assetId: 'asset-1', url: '/image.jpg' });
        expect(component['nutritionErrorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR');
        component['onReanalyze']();
        expect(component['nutrition']()).toEqual(nutrition);
        expect(component['nutritionErrorKey']()).toBeNull();
    });
});

describe('Meal photo nutrition-only session mapping', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });
    it.each(['Apple', 'Яблоко', 'Unmatched', ''])('preserves nutrition and resolves the vision name %s', async name => {
        const { component } = await setupComponentAsync();
        component['results'].set([visionItem]);
        component['nutrition'].set({ ...nutrition, items: [{ ...nutrition.items[0], name }] });
        component['addToMeal']();
        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({
                items: [expect.objectContaining({ nameEn: ['Apple', 'Яблоко'].includes(name) ? 'Apple' : name, amount: SOURCE_AMOUNT })],
            }),
        );
    });
});
