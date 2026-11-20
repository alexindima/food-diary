import { HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AiFoodFacade } from '../../../../shared/lib/ai-food.facade';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import { imageSelection } from '../../../../shared/models/image-upload.data';
import type { MealAiSessionManageDto } from '../../../../shared/models/meal.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { publicImageUrl } from '../../../../shared/models/semantics/image-location';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { MealPhotoRecognitionDialogComponent } from './meal-photo-recognition-dialog';
import { MealPhotoUploadPanelComponent } from './meal-photo-upload-panel/meal-photo-upload-panel';

const SOURCE_AMOUNT = 100;
const EDITED_AMOUNT = 150;
const BASE_CALORIES = 100;
const BASE_PROTEIN = 10;
const BASE_FAT = 5;
const BASE_CARBS = 20;
const BASE_FIBER = 2;
const EXPECTED_EDITED_CALORIES = 150;
const PARTIAL_PROTEIN_GRAMS = 3.4;
const LOW_LOCATION_CONFIDENCE = 0.2;

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

        await component['onImageChangedAsync'](imageSelection('https://example.test/photo.jpg', 'asset-1'));

        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledWith({ imageAssetId: 'asset-1' });
        expect(aiFoodService.calculateNutrition).toHaveBeenCalledWith({ items: [visionItem] });
        expect(component['results']()).toEqual([visionItem]);
        expect(component['nutrition']()).toEqual(nutrition);
        expect(component['statusKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_DONE');
    });

    it('should build photo annotations and let the user hide them', async () => {
        const { component } = await setupComponentAsync();

        await component['onImageChangedAsync'](imageSelection('https://example.test/photo.jpg', 'asset-1'));

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

    it('shows the nutrition order and keeps localized names and coordinates with the right calories', async () => {
        const tea = { ...visionItem, nameEn: 'Tea', nameLocal: 'Чай', amount: 250, unit: 'ml', centerX: 0.2 };
        aiFoodService.analyzeFoodImage.mockReturnValue(of({ items: [tea, visionItem] }));
        aiFoodService.calculateNutrition.mockReturnValue(
            of({ ...nutrition, items: [nutrition.items[0], { ...nutrition.items[0], name: 'Tea', amount: 250, unit: 'ml', calories: 1 }] }),
        );
        const { component } = await setupComponentAsync();
        await component['onImageChangedAsync'](imageSelection('https://example.test/photo.jpg', 'asset-1'));

        expect(component['resultViews']().map(item => item.displayName)).toEqual(['Яблоко', 'Чай']);
        expect(component['annotations']()).toEqual([
            expect.objectContaining({ name: 'Яблоко', centerX: 42, calories: BASE_CALORIES }),
            expect.objectContaining({ name: 'Чай', centerX: 20, calories: 1 }),
        ]);
    });

    it('should set premium error when image analysis is forbidden', async () => {
        aiFoodService.analyzeFoodImage.mockReturnValue(throwError(() => ({ status: HttpStatusCode.Forbidden })));
        const { component } = await setupComponentAsync();

        await component['onImageChangedAsync'](imageSelection(null, 'asset-1'));

        expect(component['errorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
        expect(component['hasAnalyzed']()).toBe(true);
        expect(component['isLoading']()).toBe(false);
    });

    it('should set nutrition quota error when nutrition calculation is rate limited', async () => {
        aiFoodService.calculateNutrition.mockReturnValue(throwError(() => ({ status: HttpStatusCode.TooManyRequests })));
        const { component } = await setupComponentAsync();

        await component['onImageChangedAsync'](imageSelection(null, 'asset-1'));

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
            initialSelection: imageSelection('https://example.test/photo.jpg', 'asset-1'),
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

describe('Meal AI edit invalid quantity', () => {
    it.each(['0', '-1', '', 'invalid', 'Infinity'])('keeps results and nutrition when amount %s is invalid', async amount => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
        const { component } = await setupComponentAsync();
        component['results'].set([visionItem]);
        component['nutrition'].set(nutrition);
        component['startEditing']();
        aiFoodService.calculateNutrition.mockClear();
        component['updateEditItem'](0, 'amount', amount);
        component['applyEditing']();
        expect(component['isEditing']()).toBe(true);
        expect(component['editItems']()).toHaveLength(1);
        expect(component['results']()).toEqual([visionItem]);
        expect(component['nutrition']()).toEqual(nutrition);
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        component['updateEditItem'](0, 'amount', String(EDITED_AMOUNT));
        component['applyEditing']();
        expect(component['isEditing']()).toBe(false);
        expect(component['nutrition']()?.items[0].amount).toBe(EDITED_AMOUNT);
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
                {
                    provide: PrivacyConsentFacade,
                    useValue: {
                        ensureHealthConsentAsync: vi.fn().mockResolvedValue(true),
                        failed: signal(false),
                        needsReload: signal(false),
                    },
                },
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
        await component['onImageChangedAsync'](imageSelection(null, 'first'));
        expect(component['isLoading']()).toBe(true);
        analysis.next({ items: [visionItem] });
        expect(component['isNutritionLoading']()).toBe(true);
        await component['onImageChangedAsync'](imageSelection(null, 'second'));
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
        await component['onImageChangedAsync'](imageSelection(null, 'first'));
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
    it('does not reuse review items from the previous image after clearing selection', async () => {
        const { component } = await setupComponentAsync();
        await component['onImageChangedAsync'](imageSelection(null, 'first'));
        expect(component['reviewItems']().length).toBe(1);
        await component['onImageChangedAsync'](null);
        expect(component['reviewItems']()).toEqual([]);
        component['addToMeal']();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
    it.each([HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError])(
        'clears loading and supports retry after HTTP %s',
        async status => {
            aiFoodService.analyzeFoodImage.mockReturnValueOnce(throwError(() => ({ status })));
            const { component } = await setupComponentAsync();
            await component['onImageChangedAsync'](imageSelection(null, 'first'));
            expect(component['isLoading']()).toBe(false);
            expect(component['errorKey']()).toContain(status === HttpStatusCode.TooManyRequests ? 'ERROR_QUOTA' : 'ERROR_GENERIC');
            await component['onReanalyzeAsync']();
            expect(component['errorKey']()).toBeNull();
            expect(component['nutrition']()).toEqual(nutrition);
        },
    );
    it('handles an empty recognition without calling nutrition', async () => {
        aiFoodService.analyzeFoodImage.mockReturnValue(of({ items: [] }));
        const { component } = await setupComponentAsync();
        await component['onImageChangedAsync'](imageSelection(null, 'empty'));
        expect(component['hasAnalyzed']()).toBe(true);
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        await component['onImageChangedAsync'](null);
        await component['onReanalyzeAsync']();
        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledTimes(1);
    });
});

describe('Meal photo consent and loading through uploaded selections', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('ignores consent for a replaced photo and enables adding only after nutrition completes', async () => {
        const analysis = new Subject<{ items: FoodVisionItem[] }>();
        const calculation = new Subject<FoodNutritionResponse>();
        aiFoodService.analyzeFoodImage.mockReturnValue(analysis);
        aiFoodService.calculateNutrition.mockReturnValue(calculation);
        const { fixture } = await setupComponentAsync();
        const privacy = TestBed.inject(PrivacyConsentFacade);
        let acceptOriginal: (allowed: boolean) => void = () => {};
        vi.spyOn(privacy, 'ensureHealthConsentAsync').mockReturnValueOnce(
            new Promise(resolve => {
                acceptOriginal = resolve;
            }),
        );
        const upload = fixture.debugElement.query(By.directive(MealPhotoUploadPanelComponent)).injector.get(MealPhotoUploadPanelComponent);
        const host = fixture.nativeElement as HTMLElement;
        const add = Array.from(host.querySelectorAll('button')).find(button =>
            button.textContent.includes('MEAL_MANAGE.PHOTO_AI_DIALOG.ADD_TO_MEAL'),
        );
        if (add === undefined) {
            throw new Error('Expected the Add to meal button');
        }
        const status = (): string | undefined => host.querySelector('.photo-ai-dialog__status')?.textContent.trim();

        upload.imageChanged.emit(imageSelection(null, 'original-photo'));
        fixture.detectChanges();
        expect(status()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_ANALYZING');
        expect(add.disabled).toBe(true);
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();

        upload.imageChanged.emit(imageSelection(null, 'replacement-photo'));
        await waitForAsyncTasksAsync();
        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledExactlyOnceWith({ imageAssetId: 'replacement-photo' });
        analysis.next({ items: [visionItem] });
        fixture.detectChanges();
        expect(status()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_NUTRITION');
        expect(add.disabled).toBe(true);

        acceptOriginal(true);
        await waitForAsyncTasksAsync();
        expect(aiFoodService.analyzeFoodImage).toHaveBeenCalledOnce();
        calculation.next(nutrition);
        fixture.detectChanges();

        expect(status()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.STATUS_DONE');
        expect(add.disabled).toBe(false);
        add.click();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ imageAssetId: 'replacement-photo' }));
    });

    it.each([
        { failed: false, reload: false, message: null },
        { failed: true, reload: false, message: 'PRIVACY_CHOICES.ERROR' },
        { failed: true, reload: true, message: 'PRIVACY_CHOICES.RELOAD' },
    ])('blocks analysis when consent is unavailable, failed=$failed reload=$reload', async ({ failed, reload, message }) => {
        const { fixture } = await setupComponentAsync();
        const privacy = TestBed.inject(PrivacyConsentFacade);
        vi.spyOn(privacy, 'ensureHealthConsentAsync').mockResolvedValue(false);
        privacy.failed.set(failed);
        privacy.needsReload.set(reload);
        const upload = fixture.debugElement.query(By.directive(MealPhotoUploadPanelComponent)).injector.get(MealPhotoUploadPanelComponent);

        upload.imageChanged.emit(imageSelection(null, 'meal-photo'));
        await fixture.whenStable();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const error = host.querySelector('.photo-ai-dialog__error');
        if (message === null) {
            expect(error).toBeNull();
        } else {
            expect(error?.textContent).toContain(message);
        }
        expect(host.querySelector('.photo-ai-dialog__scan-overlay')).toBeNull();
        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
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
        await component['onResumeRecognitionAsync']({
            id: entityId<'food-recognition'>('job-1'),
            imageAssetId: entityId<'image-asset'>('asset-1'),
            imageUrl: publicImageUrl('/image.jpg'),
            status: 'Succeeded',
            description: null,
            createdOnUtc: utcInstant('2026-01-01T12:00:00Z'),
            updatedOnUtc: utcInstant('2026-01-01T12:01:00Z'),
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
        await component['onImageChangedAsync'](imageSelection('/image.jpg', 'asset-1'));
        expect(component['nutritionErrorKey']()).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.NUTRITION_ERROR');
        await component['onReanalyzeAsync']();
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

describe('Meal photo rendered nutrition recovery', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('retries absent nutrition when the user corrects the amount and waits before adding the meal', async () => {
        const calculation = new Subject<FoodNutritionResponse>();
        aiFoodService.calculateNutrition
            .mockReturnValueOnce(throwError(() => new Error('nutrition temporarily unavailable')))
            .mockReturnValueOnce(calculation);
        const { fixture } = await setupComponentAsync();
        const host = fixture.nativeElement as HTMLElement;
        const upload = fixture.debugElement.query(By.directive(MealPhotoUploadPanelComponent)).injector.get(MealPhotoUploadPanelComponent);
        upload.imageChanged.emit(imageSelection(null, 'meal-photo'));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host.querySelector('.photo-ai-dialog__nutrition-error')?.textContent).toContain('NUTRITION_ERROR');
        expect(photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.ADD_TO_MEAL').disabled).toBe(true);

        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON').click();
        fixture.detectChanges();
        const amount = host.querySelector<HTMLInputElement>('.photo-ai-dialog__edit-input--amount');
        if (amount === null) {
            throw new Error('Expected the recognized amount to be editable');
        }
        amount.value = String(EDITED_AMOUNT);
        amount.dispatchEvent(new Event('input', { bubbles: true }));
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();
        fixture.detectChanges();

        expect(aiFoodService.calculateNutrition).toHaveBeenCalledTimes(2);
        expect(aiFoodService.calculateNutrition.mock.lastCall).toMatchObject([{ items: [{ amount: EDITED_AMOUNT, unit: 'g' }] }]);
        expect(host.querySelector('.photo-ai-dialog__status')?.textContent).toContain('STATUS_NUTRITION');
        expect(photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.ADD_TO_MEAL').disabled).toBe(true);
        calculation.next({
            ...nutrition,
            calories: EXPECTED_EDITED_CALORIES,
            items: [{ ...nutrition.items[0], amount: EDITED_AMOUNT, calories: EXPECTED_EDITED_CALORIES }],
        });
        calculation.complete();
        fixture.detectChanges();
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.ADD_TO_MEAL').click();
        expect(dialogRef.close.mock.lastCall).toMatchObject([
            { imageAssetId: 'meal-photo', items: [{ amount: EDITED_AMOUNT, calories: EXPECTED_EDITED_CALORIES }] },
        ]);
    });
});

describe('Meal photo review resolution', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it.each([
        { previous: 'Split', updated: 'Merged' },
        { previous: 'Merged', updated: 'Split' },
    ])('saves a changed review resolution from $previous to $updated without recalculating nutrition', async ({ previous, updated }) => {
        const session = createSession();
        const { fixture } = await setupComponentAsync({
            initialSession: { ...session, items: [{ ...session.items[0], resolution: previous }] },
            mode: 'edit',
        });
        const host = fixture.nativeElement as HTMLElement;
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON').click();
        fixture.detectChanges();
        const resolution = host.querySelector<HTMLSelectElement>('.photo-ai-dialog__edit-select--resolution');
        if (resolution === null) {
            throw new Error('Resolution selector is missing');
        }
        expect(resolution.value).toBe(previous);

        resolution.value = updated;
        resolution.dispatchEvent(new Event('change', { bubbles: true }));
        fixture.detectChanges();
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();
        fixture.detectChanges();
        expect(dialogRef.close).not.toHaveBeenCalled();
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();

        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ items: [{ ...session.items[0], resolution: updated }] }),
        );
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
    });
});

describe('Meal photo rendered saved history and annotations', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it.each([null, '   '])('restores an older session without a photo or a usable localized name (%j)', async nameLocal => {
        const original = createSession();
        const session: MealAiSessionManageDto = {
            ...original,
            imageAssetId: null,
            imageUrl: null,
            items: [
                {
                    ...original.items[0],
                    nameEn: 'oat flakes',
                    nameLocal,
                    confidence: null,
                    resolution: null,
                    proteins: PARTIAL_PROTEIN_GRAMS,
                },
            ],
        };
        const { fixture } = await setupComponentAsync({ initialSession: session, mode: 'edit' });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.photo-ai-dialog__table-body')?.textContent).toContain('Oat flakes');
        const macros = Array.from(host.querySelectorAll('.photo-ai-dialog__nutrition-value'));
        expect(macros.some(macro => macro.textContent.includes(String(PARTIAL_PROTEIN_GRAMS)))).toBe(true);
        expect(host.querySelector('.photo-ai-dialog__status')).toBeNull();
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON').click();
        fixture.detectChanges();
        expect(host.querySelector<HTMLInputElement>('.photo-ai-dialog__edit-input--name')?.value).toBe('oat flakes');
        expect(host.querySelector<HTMLSelectElement>('.photo-ai-dialog__edit-select--resolution')?.value).toBe('Accepted');
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();
        fixture.detectChanges();
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();

        expect(aiFoodService.analyzeFoodImage).not.toHaveBeenCalled();
        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(dialogRef.close.mock.lastCall).toMatchObject([
            {
                imageAssetId: null,
                imageUrl: null,
                items: [{ nameEn: 'oat flakes', nameLocal: null, confidence: 1, resolution: 'Accepted', proteins: PARTIAL_PROTEIN_GRAMS }],
            },
        ]);
    });
    it('keeps an uncertain location out of annotations while retaining the item in the nutrition table', async () => {
        const tea = { ...visionItem, nameEn: 'Tea', nameLocal: null, locationConfidence: LOW_LOCATION_CONFIDENCE };
        const apple = { ...visionItem, locationConfidence: null };
        aiFoodService.analyzeFoodImage.mockReturnValueOnce(of({ items: [tea, apple] }));
        aiFoodService.calculateNutrition.mockReturnValueOnce(
            of({
                ...nutrition,
                items: [{ ...nutrition.items[0], name: tea.nameEn }, nutrition.items[0]],
            }),
        );
        const { fixture } = await setupComponentAsync();
        const host = fixture.nativeElement as HTMLElement;
        const upload = fixture.debugElement.query(By.directive(MealPhotoUploadPanelComponent)).injector.get(MealPhotoUploadPanelComponent);
        upload.imageChanged.emit(imageSelection('https://example.test/meal.jpg', 'meal-photo'));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(host.querySelector('.photo-ai-dialog__table-body')?.textContent).toContain('Tea');
        const cards = host.querySelectorAll('.photo-ai-dialog__annotation-card');
        expect(cards).toHaveLength(1);
        expect(cards[0].textContent).toContain(visionItem.nameLocal);

        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.HIDE_ANNOTATIONS').click();
        fixture.detectChanges();
        expect(host.querySelector('.photo-ai-dialog__annotations')).toBeNull();
    });
});

function photoButton(host: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll('button')).find(candidate => candidate.textContent.trim().endsWith(label));
    if (button === undefined) {
        throw new Error(`Expected the rendered photo action "${label}"`);
    }
    return button;
}

describe('Meal photo saving same-named portions', () => {
    beforeEach(() => {
        aiFoodService = createAiFoodServiceMock();
        dialogRef = { close: vi.fn() };
    });

    it('adds separate amounts and nutrients even when the provider returns same-named portions in the reverse order', async () => {
        const largerPortionProtein = 15;
        const first = nutrition.items[0];
        const second = { ...first, amount: EDITED_AMOUNT, calories: EXPECTED_EDITED_CALORIES, protein: largerPortionProtein };
        aiFoodService.analyzeFoodImage.mockReturnValueOnce(of({ items: [visionItem, { ...visionItem, amount: EDITED_AMOUNT }] }));
        aiFoodService.calculateNutrition.mockReturnValueOnce(
            of({
                ...nutrition,
                calories: first.calories + second.calories,
                protein: first.protein + second.protein,
                items: [second, first],
            }),
        );
        const { fixture } = await setupComponentAsync();
        const host = fixture.nativeElement as HTMLElement;
        const upload = fixture.debugElement.query(By.directive(MealPhotoUploadPanelComponent)).injector.get(MealPhotoUploadPanelComponent);
        upload.imageChanged.emit(imageSelection('https://example.test/meal.jpg', 'meal-photo'));
        await fixture.whenStable();
        fixture.detectChanges();

        const annotations = Array.from(host.querySelectorAll('.photo-ai-dialog__annotation-card'));
        expect(annotations).toHaveLength(2);
        expect(annotations[0].textContent).toContain(String(EXPECTED_EDITED_CALORIES));
        expect(annotations[1].textContent).toContain(String(BASE_CALORIES));
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.ADD_TO_MEAL').click();

        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                imageAssetId: 'meal-photo',
                items: [
                    expect.objectContaining({
                        nameEn: visionItem.nameEn,
                        amount: SOURCE_AMOUNT,
                        calories: BASE_CALORIES,
                        proteins: BASE_PROTEIN,
                    }),
                    expect.objectContaining({
                        nameEn: visionItem.nameEn,
                        amount: EDITED_AMOUNT,
                        calories: EXPECTED_EDITED_CALORIES,
                        proteins: largerPortionProtein,
                    }),
                ],
            }),
        );
    });

    it('saves separately loaded portions and keeps the rejected matching row at zero', async () => {
        const original = createSession();
        const small = { ...original.items[0], amount: SOURCE_AMOUNT, calories: BASE_CALORIES, proteins: BASE_PROTEIN };
        const large = { ...small, amount: EDITED_AMOUNT, calories: EXPECTED_EDITED_CALORIES, proteins: PARTIAL_PROTEIN_GRAMS };
        const rejected = { ...small, resolution: 'Rejected', calories: 0, proteins: 0 };
        const { fixture } = await setupComponentAsync({ initialSession: { ...original, items: [rejected, small, large] }, mode: 'edit' });
        const host = fixture.nativeElement as HTMLElement;
        photoButton(host, 'MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE').click();

        expect(aiFoodService.calculateNutrition).not.toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                items: [
                    expect.objectContaining({ resolution: 'Rejected', amount: SOURCE_AMOUNT, calories: 0, proteins: 0 }),
                    expect.objectContaining({ amount: SOURCE_AMOUNT, calories: BASE_CALORIES, proteins: BASE_PROTEIN }),
                    expect.objectContaining({ amount: EDITED_AMOUNT, calories: EXPECTED_EDITED_CALORIES, proteins: PARTIAL_PROTEIN_GRAMS }),
                ],
            }),
        );
    });
});
