import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, type Params, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, firstValueFrom, of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ImageUploadFieldComponent } from '../../../../components/shared/image-upload-field/image-upload-field';
import { NavigationService } from '../../../../services/navigation.service';
import {
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
    type Meal,
    type MealAiSessionManageDto,
    type MealItem,
    type MealManageDto,
    MealSourceType,
} from '../../../../shared/models/meal.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { productQuantityFromStored, recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { MEAL_DEVELOPMENT_TOOLS, type MealDevelopmentScreen, type MealDevelopmentTools } from '../../contracts/meal-development-tools';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import { servingMassFromObservation } from '../../lib/recipe-serving/recipe-display-amount';
import { MealGeneralInfoComponent } from './meal-general-info/meal-general-info';
import { MealItemsSectionComponent } from './meal-items-section/meal-items-section';
import { MealManageFormComponent } from './meal-manage-form';
import type {
    CommittedMealItemFormValues,
    MealItemFormValues,
    MealNutritionSummaryState,
    NutritionTotals,
} from './meal-manage-lib/meal-manage.types';
import { createMealItemValue, getDateInputValue, getTimeInputValue } from './meal-manage-lib/meal-manage-form.mapper';

const PRODUCT_AMOUNT = 150;
const FRACTIONAL_RECIPE_SERVINGS = 0.5;
const RECIPE_DRAFT_MASS = servingMassFromObservation(PRODUCT_AMOUNT / FRACTIONAL_RECIPE_SERVINGS);
const TOTAL_CALORIES = 300;
const UPDATED_TOTAL_CALORIES = 450;
const NORMALIZED_SATIETY_LEVEL = 5;
const STALE_RECIPE_AMOUNT = 999;
const EXCESS_MANUAL_NUTRIENT = 10_001;
const MAX_COMMENT_LENGTH = 2048;
const EMPTY_TOTALS: NutritionTotals = {
    calories: 0,
    proteins: 0,
    fats: 0,
    carbs: 0,
    fiber: 0,
    alcohol: 0,
};

type MealManageFacadeMock = {
    prepareRecipeItemAsync: ReturnType<typeof vi.fn<MealManageFacade['prepareRecipeItemAsync']>>;
    addAiSession: ReturnType<typeof vi.fn>;
    buildManualNutritionPatchFromTotals: ReturnType<typeof vi.fn>;
    buildNutritionSummaryStateFromValues: ReturnType<typeof vi.fn>;
    configureItemType: ReturnType<typeof vi.fn>;
    confirmDiscardChangesAsync: ReturnType<typeof vi.fn>;
    createMealItem: ReturnType<typeof vi.fn<MealManageFacade['createMealItem']>>;
    ensurePremiumAccess: ReturnType<typeof vi.fn>;
    getManualNutritionTotalsFromValue: ReturnType<typeof vi.fn>;
    openEditAiPhotoSessionDialogAsync: ReturnType<typeof vi.fn>;
    prepareMealItems: ReturnType<typeof vi.fn>;
    removeAiSession: ReturnType<typeof vi.fn>;
    replaceAiSession: ReturnType<typeof vi.fn>;
    showSuccessToastAndRedirectAsync: ReturnType<typeof vi.fn<MealManageFacade['showSuccessToastAndRedirectAsync']>>;
    submitMealAsync: ReturnType<typeof vi.fn>;
};

type MealManageFormSetup = {
    component: MealManageFormComponent;
    fixture: ComponentFixture<MealManageFormComponent>;
    openDialog: ReturnType<typeof vi.fn>;
    mealManageFacade: MealManageFacadeMock;
    navigationService: {
        navigateToMealListAsync: ReturnType<typeof vi.fn>;
    };
};

describe('MealManageFormComponent input behavior', () => {
    it('should repopulate form when meal with the same id is refreshed', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.buildNutritionSummaryStateFromValues
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(TOTAL_CALORIES))
            .mockReturnValueOnce(createNutritionSummaryStateWithCalories(UPDATED_TOTAL_CALORIES));

        fixture.componentRef.setInput('meal', createMeal({ totalCalories: TOTAL_CALORIES }));
        fixture.detectChanges();
        expect(component['mealFormModel']().comment).toBe('Comment');

        fixture.componentRef.setInput('meal', createMeal({ totalCalories: UPDATED_TOTAL_CALORIES }));
        fixture.detectChanges();

        expect(component['mealFormModel']().comment).toBe('Updated comment');
    });
});

describe('MealManageFormComponent comment boundary', () => {
    it('rejects an oversized comment before saving and preserves the draft', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('meal', createMeal({ comment: 'Original' }));
        fixture.detectChanges();
        const comment = 'x'.repeat(MAX_COMMENT_LENGTH + 1);
        component['patchMealFormModel']({ comment, items: [createCommentTestItem()] });
        component['mealSignalForm'].comment().markAsTouched();
        fixture.detectChanges();

        await component['onSubmitAsync']();

        expect(component['mealSignalForm']().invalid()).toBe(true);
        expect(component['mealSignalForm'].comment().errors()).toEqual([
            expect.objectContaining({ kind: 'maxLength', maxLength: MAX_COMMENT_LENGTH }),
        ]);
        expect(component['generalFieldErrors']().comment).not.toBeNull();
        expect(component['mealFormModel']().comment).toBe(comment);
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });

    it.each([null, '', '   ', `  ${'x'.repeat(MAX_COMMENT_LENGTH)}  `])('allows a clear or normalized boundary comment', async comment => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        fixture.componentRef.setInput('meal', createMeal());
        fixture.detectChanges();
        component['patchMealFormModel']({ comment, items: [createCommentTestItem()] });
        fixture.detectChanges();

        await component['onSubmitAsync']();

        expect(component['mealSignalForm'].comment().invalid()).toBe(false);
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
    });
});

function createCommentTestItem(): CommittedMealItemFormValues {
    return createMealItemValue(
        { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
        null,
        PRODUCT_AMOUNT,
        MealSourceType.Product,
    );
}

describe('MealManageFormComponent native submit behavior', () => {
    it('should prevent native form submit when saving', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitMealAsync.mockResolvedValue(createMeal({ totalCalories: TOTAL_CALORIES }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
    });
});

describe('MealManageFormComponent health consent', () => {
    it.each([
        { failed: false, reload: false, message: null },
        { failed: true, reload: false, message: 'PRIVACY_CHOICES.ERROR' },
        { failed: true, reload: true, message: 'PRIVACY_CHOICES.RELOAD' },
    ])('preserves the draft when consent is unavailable, failed=$failed reload=$reload', async ({ failed, reload, message }) => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        const privacy = TestBed.inject(PrivacyConsentFacade);
        vi.spyOn(privacy, 'ensureHealthConsentAsync').mockResolvedValue(false);
        privacy.failed.set(failed);
        privacy.needsReload.set(reload);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const note = host.querySelector<HTMLTextAreaElement>('textarea');
        if (note?.form === null || note?.form === undefined) {
            throw new Error('Expected the meal comment and form');
        }
        note.value = 'Keep this draft';
        note.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        note.form.requestSubmit();
        await fixture.whenStable();
        fixture.detectChanges();

        const error = host.querySelector('.meal-manage__footer-error');
        if (message === null) {
            expect(error).toBeNull();
        } else {
            expect(error?.textContent).toContain(message);
        }
        expect(note.value).toBe('Keep this draft');
        expect(host.querySelector('form')?.getAttribute('aria-busy')).toBe('false');
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });

    it.each([true, false])('waits for consent before sending a meal, granted=%s', async granted => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitMealAsync.mockResolvedValue(createMeal({ totalCalories: TOTAL_CALORIES }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            comment: 'Lunch',
        });
        component['patchMealFormModel']({
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        const privacy = TestBed.inject(PrivacyConsentFacade);
        let grantConsent: (allowed: boolean) => void = () => {};
        const review = vi.spyOn(privacy, 'ensureHealthConsentAsync');
        review.mockReturnValueOnce(
            new Promise(resolve => {
                grantConsent = resolve;
            }),
        );

        const submission = component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(review).toHaveBeenCalledWith(true, { acceptLabelKey: 'PRIVACY_CHOICES.ACCEPT_AND_SAVE' });
        grantConsent(granted);
        await submission;

        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledTimes(Number(granted));
        expect(component['globalError']()).toBeNull();
    });
});

describe('MealManageFormComponent submit behavior', () => {
    it('should submit create DTO and reset add form after successful create', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitMealAsync.mockResolvedValue(createMeal({ totalCalories: TOTAL_CALORIES }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            comment: 'Lunch',
        });
        component['patchMealFormModel']({
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledWith(
            null,
            expect.objectContaining<Partial<MealManageDto>>({
                mealType: 'BREAKFAST',
                comment: 'Lunch',
                isNutritionAutoCalculated: true,
                items: [{ productId: 'product-1', recipeId: null, amount: PRODUCT_AMOUNT, origin: 'Manual' }],
            }),
            expect.any(String),
        );
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).toHaveBeenCalledWith(false);
        expect(component['aiSessions']()).toEqual([]);
        expect(component['items'].length).toBe(1);
    });

    it('should show global error and skip submit when form is invalid', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        component['patchMealFormModel']({
            date: '',
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(component['globalError']()).toBe('FORM_ERRORS.CHECK_FIELDS');
    });

    it('should show backend validation message when submit fails', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const serverMessage = 'Product is not accessible.';
        mealManageFacade.submitMealAsync.mockRejectedValue(new HttpErrorResponse({ status: 400, error: { message: serverMessage } }));
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
        });
        component['patchMealFormModel']({
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();
        await waitForAsyncTasksAsync();

        expect(component['globalError']()).toBe(serverMessage);
    });
});

describe('MealManageFormComponent network recovery', () => {
    it('keeps the meal draft and localizes a network failure instead of exposing fetch internals', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.submitMealAsync.mockRejectedValue(new HttpErrorResponse({ status: 0, error: new TypeError('Failed to fetch') }));
        const items = [
            createMealItemValue(
                { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                null,
                PRODUCT_AMOUNT,
                MealSourceType.Product,
            ),
        ];
        component['patchMealFormModel']({ date: '2026-04-05', time: '10:30', mealType: 'BREAKFAST', items });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(component['globalError']()).toBe('FORM_ERRORS.SAVE_CONNECTION');
        expect(component['mealFormModel']().items).toEqual(items);
    });
});

describe('MealManageFormComponent item validation', () => {
    it('should show a specific error and skip submit when no items are added', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            items: [],
        });

        await component['onSubmitAsync']();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(component['globalError']()).toBe('FORM_ERRORS.NON_EMPTY_ARRAY');
    });
});

describe('MealManageFormComponent duplicate submit guard', () => {
    it('should ignore repeated submit while create is in progress', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        let resolveSubmit: ((value: Meal) => void) | undefined;
        mealManageFacade.submitMealAsync.mockReturnValue(
            new Promise<Meal>(resolve => {
                resolveSubmit = resolve;
            }),
        );
        component['patchMealFormModel']({
            date: '2026-04-05',
            time: '10:30',
            mealType: 'BREAKFAST',
            items: [
                createMealItemValue(
                    { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                    null,
                    PRODUCT_AMOUNT,
                    MealSourceType.Product,
                ),
            ],
        });

        const firstSubmit = component['onSubmitAsync']();
        await component['onSubmitAsync']();

        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
        expect(component['isSubmitting']()).toBe(true);

        resolveSubmit?.(createMeal({ totalCalories: TOTAL_CALORIES }));
        await firstSubmit;

        expect(component['isSubmitting']()).toBe(false);
    });
});

describe('Meal editor pending save', () => {
    it.each([false, true])('locks all editable controls until save settles, failure=%s', async fail => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['mealSignalForm'].mealType().markAsDirty();
        let settle: (() => void) | undefined;
        mealManageFacade.submitMealAsync.mockReturnValue(
            new Promise<Meal>((resolve, reject) => {
                settle = (): void => {
                    if (fail) {
                        reject(new Error('Temporary save failure'));
                    } else {
                        resolve(createMeal({ totalCalories: TOTAL_CALORIES }));
                    }
                };
            }),
        );
        const draft = component['mealFormModel']();
        const saving = component['onSubmitAsync']();
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        const controls = [...element.querySelectorAll('form input, form textarea, form select, form button')];
        expect(controls.length).toBeGreaterThan(0);
        expect(controls.every(control => control.matches(':disabled'))).toBe(true);
        expect(element.querySelector('fd-page-header')?.hasAttribute('inert')).toBe(true);

        settle?.();
        await saving;
        fixture.detectChanges();
        expect(controls.some(control => !control.matches(':disabled'))).toBe(true);
        if (fail) {
            expect(component['mealFormModel']()).toEqual(draft);
            expect(component['mealSignalForm']().dirty()).toBe(true);
            expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
        }
    });
});

describe('Meal editor photo readiness', () => {
    it.each([false, true])('blocks submission while uploading and re-enables it after failure or replacement, edit=%s', async editing => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        if (editing) {
            fixture.componentRef.setInput('meal', createMeal());
            fixture.detectChanges();
        }
        setValidManualMeal(component);
        component['mealSignalForm'].mealType().markAsDirty();
        fixture.detectChanges();
        const generalInfo = fixture.debugElement.query(By.directive(MealGeneralInfoComponent));
        const uploader = generalInfo.query(By.directive(ImageUploadFieldComponent)).componentInstance as ImageUploadFieldComponent;
        const draft = component['mealFormModel']();

        uploader.imagePreparationStarted.emit('blob:owned-photo');
        fixture.detectChanges();
        expect(component['isSubmitDisabled']()).toBe(true);
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).querySelector('textarea')?.matches(':disabled')).toBe(false);

        uploader.imagePreparationFailed.emit();
        fixture.detectChanges();
        expect(component['isSubmitDisabled']()).toBe(false);
        expect(component['mealFormModel']()).toEqual(draft);
        uploader.imagePreparationStarted.emit('blob:replacement-photo');
        fixture.detectChanges();
        expect(component['isSubmitDisabled']()).toBe(true);
        uploader.imageChanged.emit(null);
        fixture.detectChanges();
        expect(component['isSubmitDisabled']()).toBe(false);
    });
});

describe('MealManageFormComponent create retry identity', () => {
    it('retains the key after failure and replaces it for changed or successfully completed drafts', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        const items = [
            createMealItemValue(
                { ...createEmptyProductSnapshot(), id: entityId<'product'>('product-1'), name: 'Apple' },
                null,
                PRODUCT_AMOUNT,
                MealSourceType.Product,
            ),
        ];
        const draft = { date: '2026-04-05', time: '10:30', mealType: 'BREAKFAST', items };
        component['patchMealFormModel'](draft);
        mealManageFacade.submitMealAsync.mockRejectedValueOnce(new Error('Response lost'));
        await component['onSubmitAsync']();
        const firstKey: unknown = mealManageFacade.submitMealAsync.mock.calls[0][2];
        expect(firstKey).toEqual(expect.any(String));
        mealManageFacade.submitMealAsync.mockRejectedValueOnce(new Error('Still offline'));
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync.mock.calls[1][2]).toBe(firstKey);

        component['patchMealFormModel']({ comment: 'Changed intent' });
        mealManageFacade.submitMealAsync.mockResolvedValueOnce(createMeal({ totalCalories: TOTAL_CALORIES }));
        await component['onSubmitAsync']();
        const changedKey: unknown = mealManageFacade.submitMealAsync.mock.calls[2][2];
        expect(changedKey).not.toBe(firstKey);

        component['patchMealFormModel']({ ...draft, comment: 'Changed intent' });
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync.mock.calls[3][2]).not.toBe(changedKey);
    });
});

describe('MealManageFormComponent item and AI behavior', () => {
    it('should open manual item dialog for a reusable empty item', async () => {
        const { component } = await setupComponentAsync();

        component['addMealItem']();
        await waitForAsyncTasksAsync();

        expect(component['items'].length).toBe(1);
    });

    it('appends a recognition and removes it through its rendered action', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const session: MealAiSessionManageDto = { notes: 'recognized', items: [] };
        mealManageFacade.addAiSession.mockReturnValue([session]);
        mealManageFacade.removeAiSession.mockReturnValue([]);

        component['onAiMealRecognized']({
            source: 'Photo',
            imageAssetId: null,
            imageUrl: null,
            recognizedAtUtc: '2026-04-05T10:30:00Z',
            notes: 'recognized',
            items: [],
        });
        expect(component['aiSessions']()).toEqual([session]);

        fixture.detectChanges();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.ITEMS_AI_DELETE"]').click();

        expect(component['aiSessions']()).toEqual([]);
    });

    it('should skip AI session edit when premium access is rejected', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.ensurePremiumAccess.mockReturnValueOnce(false);
        component['aiSessions'].set([{ notes: 'recognized', items: [] }]);

        fixture.detectChanges();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.ITEMS_AI_EDIT"]').click();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).not.toHaveBeenCalled();
        expect(mealManageFacade.replaceAiSession).not.toHaveBeenCalled();
    });

    it('should replace AI session after successful edit', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const session: MealAiSessionManageDto = { notes: 'recognized', items: [] };
        const updatedSession: MealAiSessionManageDto = { notes: 'updated', items: [] };
        component['aiSessions'].set([session]);
        mealManageFacade.openEditAiPhotoSessionDialogAsync.mockResolvedValueOnce(updatedSession);
        mealManageFacade.replaceAiSession.mockReturnValueOnce([updatedSession]);

        fixture.detectChanges();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.ITEMS_AI_EDIT"]').click();
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).toHaveBeenCalledWith(
            session,
            expect.anything(),
            expect.any(AbortSignal),
        );
        expect(component['aiSessions']()).toEqual([updatedSession]);
    });
});

describe('MealManageFormComponent nutrition and satiety behavior', () => {
    it('should switch to manual nutrition and populate manual values from current totals', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        mealManageFacade.buildNutritionSummaryStateFromValues.mockReturnValue(createNutritionSummaryStateWithCalories(TOTAL_CALORIES));

        component['onNutritionModeChange']('manual');

        expect(component['nutritionMode']()).toBe('manual');
        expect(component['mealFormModel']().isNutritionAutoCalculated).toBe(false);
        expect(component['mealFormModel']().manualCalories).toBe(TOTAL_CALORIES);
    });

    it('should normalize satiety level changes and mark control dirty', async () => {
        const { component } = await setupComponentAsync();

        component['onSatietyLevelChange']('preMealSatietyLevel', NORMALIZED_SATIETY_LEVEL);

        expect(component['mealFormModel']().preMealSatietyLevel).toBe(NORMALIZED_SATIETY_LEVEL);
        expect(component['preMealSatietyLevel']()).toBe(NORMALIZED_SATIETY_LEVEL);
    });
});

describe('MealManageFormComponent navigation', () => {
    it('should navigate to meal list on cancel', async () => {
        const { component, navigationService } = await setupComponentAsync();

        await component['onCancelAsync']();

        expect(navigationService.navigateToMealListAsync).toHaveBeenCalled();
    });

    it('should stay on the form when discarding dirty changes is cancelled', async () => {
        const { component, mealManageFacade, navigationService } = await setupComponentAsync();
        component['mealSignalForm'].comment().markAsDirty();
        mealManageFacade.confirmDiscardChangesAsync.mockResolvedValueOnce(false);

        await component['onCancelAsync']();

        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToMealListAsync).not.toHaveBeenCalled();
    });

    it('should leave the form when discarding dirty changes is confirmed', async () => {
        const { component, mealManageFacade, navigationService } = await setupComponentAsync();
        component['mealSignalForm'].comment().markAsDirty();

        await component['onCancelAsync']();

        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToMealListAsync).toHaveBeenCalledOnce();
    });
});

describe('Meal editor route departure', () => {
    it('keeps the draft after failed save, blocks pending save, and permits the committed edit redirect', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const meal = mealWithProduct('departure-meal', 'Original note');
        let failSave: ((response: null) => void) | undefined;
        mealManageFacade.submitMealAsync
            .mockReturnValueOnce(
                new Promise<Meal | null>(resolve => {
                    failSave = resolve;
                }),
            )
            .mockResolvedValueOnce(meal);
        mealManageFacade.confirmDiscardChangesAsync.mockResolvedValueOnce(false);
        mealManageFacade.showSuccessToastAndRedirectAsync.mockImplementationOnce(async () => {
            expect(await component.canLeaveAsync()).toBe(true);
        });
        fixture.componentRef.setInput('meal', meal);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const note = host.querySelector('textarea');
        if (note === null) {
            throw new Error('Expected the rendered meal editor comment');
        }
        note.value = 'Edited note';
        note.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const save = requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.SAVE_BUTTON"]');
        save.click();
        await vi.waitFor(() => {
            expect(mealManageFacade.submitMealAsync).toHaveBeenCalledTimes(1);
        });
        expect(await component.canLeaveAsync()).toBe(false);
        expect(mealManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();

        failSave?.(null);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('.meal-manage__footer-error')?.textContent).toContain('QUICK_MEAL.SAVE_ERROR');
        });
        expect(note.value).toBe('Edited note');
        expect(await component.canLeaveAsync()).toBe(false);
        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledTimes(1);

        save.click();
        await fixture.whenStable();
        expect(mealManageFacade.submitMealAsync).toHaveBeenLastCalledWith(meal, expect.objectContaining({ comment: 'Edited note' }));
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).toHaveBeenCalledWith(true);
        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledTimes(1);
    });
});

async function setupComponentAsync(
    developmentTools?: MealDevelopmentTools,
    launch?: { query: Params; picker: Subject<MealItemFormValues | null> },
): Promise<MealManageFormSetup> {
    const mealManageFacade = createMealManageFacadeMock();
    const openDialog = vi.fn().mockReturnValue({ afterClosed: () => launch?.picker ?? of(true) });
    const navigationService = {
        navigateToMealListAsync: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
        imports: [MealManageFormComponent],
        providers: [
            ...(developmentTools === undefined ? [] : [{ provide: MEAL_DEVELOPMENT_TOOLS, useValue: developmentTools }]),
            provideTranslateTesting(),
            { provide: MealManageFacade, useValue: mealManageFacade },
            {
                provide: PrivacyConsentFacade,
                useValue: {
                    ensureHealthConsentAsync: vi.fn().mockResolvedValue(true),
                    failed: signal(false),
                    needsReload: signal(false),
                    state: signal<ReturnType<PrivacyConsentFacade['state']>>(null),
                },
            },
            { provide: NavigationService, useValue: navigationService },
            {
                provide: FdUiDialogService,
                useValue: { open: openDialog },
            },
            {
                provide: Router,
                useValue: {
                    currentNavigation: vi.fn().mockReturnValue(null),
                    events: EMPTY,
                },
            },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: convertToParamMap(launch?.query ?? {}),
                    },
                },
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealManageFormComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
        openDialog,
        mealManageFacade,
        navigationService,
    };
}

describe('Meal editor development capability', () => {
    it('uses the owner conversion for recipe servings and aborts if the editor changes during conversion', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const { component, mealManageFacade } = await setupComponentAsync({
            attach: (screen: MealDevelopmentScreen): (() => void) => {
                screens.push(screen);
                return (): void => {};
            },
        });
        const recipe = createEmptyRecipeSnapshot();
        let complete: ((value: CommittedMealItemFormValues) => void) | undefined;
        mealManageFacade.prepareRecipeItemAsync.mockImplementationOnce(
            async () =>
                new Promise(resolve => {
                    complete = resolve;
                }),
        );
        const pending = screens[0].fillDraft?.(
            {
                date: '2026-10-17',
                time: '12:00',
                mealType: 'LUNCH',
                comment: '',
                items: [{ recipe, servings: recipeServingsFromStored(FRACTIONAL_RECIPE_SERVINGS) }],
            },
            vi.fn(),
        );
        component['patchMealFormModel']({ comment: 'User typed during lookup' });
        complete?.(createRecipeEditorItem(recipe, PRODUCT_AMOUNT));
        await expect(pending).rejects.toThrow('changed');
        expect(component['mealFormModel']().comment).toBe('User typed during lookup');
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });

    it('fills the owner display amount while retaining an explicit recipe serving meaning', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const { component, mealManageFacade } = await setupComponentAsync({
            attach: (screen: MealDevelopmentScreen): (() => void) => {
                screens.push(screen);
                return (): void => {};
            },
        });
        const recipe = createEmptyRecipeSnapshot();
        await screens[0].fillDraft?.(
            {
                date: '2026-10-17',
                time: '12:00',
                mealType: 'LUNCH',
                comment: '',
                items: [{ recipe, servings: recipeServingsFromStored(FRACTIONAL_RECIPE_SERVINGS) }],
            },
            vi.fn(),
        );
        expect(mealManageFacade.prepareRecipeItemAsync).toHaveBeenCalledWith(recipe, FRACTIONAL_RECIPE_SERVINGS);
        expect(component['mealFormModel']().items[0]).toMatchObject({
            sourceType: MealSourceType.Recipe,
            recipe,
            product: null,
            amount: PRODUCT_AMOUNT,
            recipeMass: RECIPE_DRAFT_MASS,
        });
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

describe('Meal editor development draft guards', () => {
    it('fills a dirty, unsaved draft and keeps the existing leave guard and submit boundary', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const dispose = vi.fn();
        const bridge: MealDevelopmentTools = {
            attach: screen => {
                screens.push(screen);
                return dispose;
            },
        };
        const { component, fixture, mealManageFacade } = await setupComponentAsync(bridge);
        const screen = screens[0];
        const requireSession = vi.fn();
        await screen.fillDraft?.(
            {
                date: '2026-10-17',
                time: '00:15',
                mealType: 'OTHER',
                comment: 'WebMCP draft',
                items: [{ product: { ...createEmptyProductSnapshot(), name: 'Pilot apple' }, amount: 150 }],
            },
            requireSession,
        );
        fixture.detectChanges();
        expect(requireSession).toHaveBeenCalledOnce();
        expect(component['mealFormModel']()).toMatchObject({
            date: '2026-10-17',
            time: '00:15',
            mealType: 'OTHER',
            comment: 'WebMCP draft',
        });
        expect(screen.isDirty?.()).toBe(true);
        expect(screen.read()).toMatchObject({
            kind: 'new-meal-editor',
            dirty: true,
            items: [{ name: 'Pilot apple', amount: 150, unit: 'G' }],
        });
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        await expect(
            screen.fillDraft?.({ date: '2026-10-18', time: '12:00', mealType: 'LUNCH', comment: '', items: [] }, requireSession),
        ).rejects.toThrow('clean');
        await component.canLeaveAsync();
        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        fixture.destroy();
        expect(dispose).toHaveBeenCalledOnce();
    });
});

function createMealManageFacadeMock(): MealManageFacadeMock {
    const facade: MealManageFacadeMock = {
        prepareRecipeItemAsync: vi
            .fn<MealManageFacade['prepareRecipeItemAsync']>()
            .mockImplementation(async recipe => firstValueFrom(of(createRecipeEditorItem(recipe, PRODUCT_AMOUNT)))),
        addAiSession: vi.fn((_sessions: MealAiSessionManageDto[], session: MealAiSessionManageDto) => [session]),
        buildManualNutritionPatchFromTotals: vi.fn((totals: NutritionTotals) => ({
            manualCalories: totals.calories,
            manualProteins: totals.proteins,
            manualFats: totals.fats,
            manualCarbs: totals.carbs,
            manualFiber: totals.fiber,
            manualAlcohol: totals.alcohol,
        })),
        buildNutritionSummaryStateFromValues: vi.fn((_formValue, _aiSessions, _threshold) => createNutritionSummaryState()),
        confirmDiscardChangesAsync: vi.fn().mockResolvedValue(true),
        configureItemType: vi.fn((item: MealItemFormValues) => item),
        createMealItem: vi.fn(() => createMealItemValue()),
        ensurePremiumAccess: vi.fn().mockReturnValue(true),
        getManualNutritionTotalsFromValue: vi.fn().mockReturnValue(EMPTY_TOTALS),
        openEditAiPhotoSessionDialogAsync: vi.fn().mockResolvedValue(null),
        prepareMealItems: vi.fn(),
        removeAiSession: vi.fn((sessions: MealAiSessionManageDto[], index: number) =>
            sessions.filter((_session, currentIndex) => currentIndex !== index),
        ),
        replaceAiSession: vi.fn(),
        showSuccessToastAndRedirectAsync: vi.fn<MealManageFacade['showSuccessToastAndRedirectAsync']>().mockResolvedValue(void 0),
        submitMealAsync: vi.fn().mockResolvedValue(null),
    };
    facade.prepareMealItems.mockImplementation((items: MealItem[]) =>
        of(
            items.length === 0
                ? [facade.createMealItem()]
                : items.map(item => {
                      return createMealItemValue(item.product, item.recipe, item.amount, item.sourceType);
                  }),
        ),
    );
    return facade;
}

function createNutritionSummaryState(): MealNutritionSummaryState {
    return createNutritionSummaryStateWithCalories(0);
}

function createRecipeEditorItem(recipe: ReturnType<typeof createEmptyRecipeSnapshot>, amount: number): CommittedMealItemFormValues {
    return { ...createMealItemValue(null, recipe, amount, MealSourceType.Recipe), recipeMass: RECIPE_DRAFT_MASS };
}

function createNutritionSummaryStateWithCalories(calories: number): MealNutritionSummaryState {
    return {
        autoTotals: {
            ...EMPTY_TOTALS,
            calories,
        },
        summaryTotals: {
            ...EMPTY_TOTALS,
            calories,
        },
        warning: null,
    };
}

function createMeal(overrides: Partial<Meal> = {}): Meal {
    const totalCalories = overrides.totalCalories ?? TOTAL_CALORIES;
    return {
        id: entityId<'meal'>('meal-1'),
        date: utcInstant('2026-04-05T10:30:00'),
        mealType: 'Breakfast',
        comment: totalCalories === UPDATED_TOTAL_CALORIES ? 'Updated comment' : 'Comment',
        totalCalories,
        totalProteins: 20,
        totalFats: 10,
        totalCarbs: 30,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
        ...overrides,
    };
}

describe('MealManageForm recipe amount loading', () => {
    it('keeps a cold recipe editor unavailable until its weight has loaded', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const prepared = new Subject<MealItemFormValues[]>();
        mealManageFacade.prepareMealItems.mockReturnValue(prepared);
        const recipe = { ...createEmptyRecipeSnapshot(), id: entityId<'recipe'>('r'), name: 'Soup' };
        const original = createMeal({
            items: [
                {
                    id: entityId<'meal-item'>('i'),
                    mealId: entityId<'meal'>('meal-1'),
                    amount: recipeServingsFromStored(2),
                    sourceType: MealSourceType.Recipe,
                    recipe,
                    product: null,
                },
            ],
        });
        fixture.componentRef.setInput('meal', original);
        fixture.detectChanges();
        const nativeElement = fixture.nativeElement as HTMLElement;
        const formElement = nativeElement.querySelector('form');

        expect(component['isLoadingRecipeAmounts']()).toBe(true);
        expect(component['isSubmitDisabled']()).toBe(true);
        expect(formElement?.style.display).toBe('none');
        expect(nativeElement.querySelector('fd-page-body [role="status"]')?.getAttribute('aria-label')).toBe('COMMON.LOADING');
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();

        prepared.next([createMealItemValue(null, recipe, PRODUCT_AMOUNT, MealSourceType.Recipe)]);
        fixture.detectChanges();
        expect(component['isLoadingRecipeAmounts']()).toBe(false);
        expect(component['mealFormModel']().items[0].amount).toBe(PRODUCT_AMOUNT);
        expect(component['mealSignalForm']().dirty()).toBe(false);
        expect(formElement?.style.display).toBe('');
        await component['onCancelAsync']();
        expect(mealManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
    });
    it.each(['refresh', 'reset', 'destroy'] as const)('ignores an obsolete recipe weight response after %s', async action => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const prepared = new Subject<MealItemFormValues[]>();
        mealManageFacade.prepareMealItems.mockReturnValueOnce(prepared);
        fixture.componentRef.setInput('meal', createMeal());
        fixture.detectChanges();

        if (action === 'destroy') {
            fixture.destroy();
        } else {
            fixture.componentRef.setInput('meal', action === 'reset' ? null : createMeal({ comment: 'Newer meal' }));
            fixture.detectChanges();
        }
        const currentItems = component['mealFormModel']().items;
        prepared.next([createMealItemValue(null, createEmptyRecipeSnapshot(), STALE_RECIPE_AMOUNT, MealSourceType.Recipe)]);
        expect(prepared.observed).toBe(false);
        expect(component['mealFormModel']().items).toEqual(currentItems);
        if (action !== 'destroy') {
            expect(component['isLoadingRecipeAmounts']()).toBe(false);
        }
    });
});

describe('MealManageForm existing meal editing', () => {
    it.each([false, true])('submits an existing product meal and preserves entered values on failure=%s', async fail => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const product = { ...createEmptyProductSnapshot(), id: entityId<'product'>('p'), name: 'Apple' };
        const original = createMeal({
            items: [
                {
                    id: entityId<'meal-item'>('i'),
                    mealId: entityId<'meal'>('meal-1'),
                    amount: productQuantityFromStored(PRODUCT_AMOUNT),
                    sourceType: MealSourceType.Product,
                    product,
                    recipe: null,
                },
            ],
        });
        mealManageFacade.createMealItem.mockImplementation(createMealItemValue);
        fixture.componentRef.setInput('meal', original);
        fixture.detectChanges();
        expect(component['mealFormModel']().items[0]).toMatchObject({ product, amount: PRODUCT_AMOUNT });
        component['patchMealFormModel']({ comment: 'Edited note' });
        if (fail) {
            mealManageFacade.submitMealAsync.mockRejectedValue(new Error('offline'));
        } else {
            mealManageFacade.submitMealAsync.mockResolvedValue(original);
        }
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledWith(
            original,
            expect.objectContaining({
                comment: 'Edited note',
                items: [expect.objectContaining({ productId: 'p', amount: PRODUCT_AMOUNT })],
            }),
        );
        expect(component['mealFormModel']().comment).toBe('Edited note');
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).toHaveBeenCalledTimes(fail ? 0 : 1);
    });
    it('converts saved recipe servings into editable grams and back into the update payload', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const recipe = { ...createEmptyRecipeSnapshot(), id: entityId<'recipe'>('r'), name: 'Soup' };
        const savedServings = 2;
        const original = createMeal({
            items: [
                {
                    id: entityId<'meal-item'>('i'),
                    mealId: entityId<'meal'>('meal-1'),
                    amount: recipeServingsFromStored(savedServings),
                    sourceType: MealSourceType.Recipe,
                    recipe,
                    product: null,
                },
            ],
        });
        mealManageFacade.createMealItem.mockImplementation(createMealItemValue);
        const mass = servingMassFromObservation(PRODUCT_AMOUNT / savedServings);
        mealManageFacade.prepareMealItems.mockReturnValueOnce(
            of([{ ...createMealItemValue(null, recipe, PRODUCT_AMOUNT, MealSourceType.Recipe), recipeMass: mass }]),
        );
        fixture.componentRef.setInput('meal', original);
        fixture.detectChanges();
        expect(component['mealFormModel']().items[0].amount).toBe(PRODUCT_AMOUNT);
        mealManageFacade.submitMealAsync.mockResolvedValue(original);
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledWith(
            original,
            expect.objectContaining({ items: [expect.objectContaining({ recipeId: 'r', amount: savedServings })] }),
        );
    });
});

describe('MealManageForm manual item dialog', () => {
    it.each([true, false])('updates an item only when selection is accepted=%s', async accepted => {
        const { component, fixture } = await setupComponentAsync();
        const original = createMealItemValue(
            { ...createEmptyProductSnapshot(), id: entityId<'product'>('original') },
            null,
            PRODUCT_AMOUNT,
        );
        const selected = createMealItemValue({ ...createEmptyProductSnapshot(), id: entityId<'product'>('new') }, null, PRODUCT_AMOUNT);
        component['patchMealFormModel']({ items: [original] });
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.spyOn(dialogs, 'open').mockReturnValue({ afterClosed: () => of(accepted ? selected : null) } as unknown as ReturnType<
            typeof dialogs.open
        >);
        component['onItemSourceClick'](0);
        await waitForAsyncTasksAsync();
        expect(component['mealFormModel']().items).toEqual([accepted ? selected : original]);
        fixture.detectChanges();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]').click();
        expect(component['mealFormModel']().items).toEqual([]);
    });
});

describe('Meal form native nutrition validation messages', () => {
    it.each([{ manualCalories: -1 }, { manualFiber: -1 }, { manualProteins: EXCESS_MANUAL_NUTRIENT }])(
        'explains invalid manual values on native submission: %j',
        async patch => {
            const { component, fixture, mealManageFacade } = await setupComponentAsync();
            setValidManualMeal(component);
            component['patchMealFormModel'](patch);
            fixture.detectChanges();

            (fixture.nativeElement as HTMLElement)
                .querySelector('form')
                ?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
            await fixture.whenStable();
            fixture.detectChanges();

            expect(component['globalError']()).toBe('FORM_ERRORS.CHECK_FIELDS');
            expect((fixture.nativeElement as HTMLElement).querySelector('.meal-manage__footer-error')?.textContent).toContain(
                'FORM_ERRORS.CHECK_FIELDS',
            );
            expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        },
    );
});

describe('Meal form manual nutrition validation', () => {
    it.each([null, -1, Number.NaN, Number.POSITIVE_INFINITY])('does not submit invalid manual calories %s', async manualCalories => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['patchMealFormModel']({ manualCalories });
        fixture.detectChanges();
        await component['onSubmitAsync']();
        expect(component['caloriesError']()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED');
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
    it.each(['manualProteins', 'manualFats', 'manualCarbs', 'manualFiber', 'manualAlcohol'] as const)(
        'rejects a negative %s',
        async field => {
            const { component, fixture, mealManageFacade } = await setupComponentAsync();
            setValidManualMeal(component);
            component['patchMealFormModel']({ [field]: -1, manualCalories: TOTAL_CALORIES, manualCarbs: PRODUCT_AMOUNT });
            if (field === 'manualCarbs') {
                component['patchMealFormModel']({ manualCarbs: -1 });
            }
            fixture.detectChanges();
            await component['onSubmitAsync']();
            expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
            expect(component['globalError']()).not.toBeNull();
        },
    );
    it('shows macro validation and preserves the draft when all macros are missing', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['patchMealFormModel']({
            manualProteins: null,
            manualFats: null,
            manualCarbs: null,
            manualFiber: null,
            manualAlcohol: null,
        });
        await component['onSubmitAsync']();
        expect(component['macrosError']()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(component['mealFormModel']().items).toHaveLength(1);
    });

    it('submits explicit zero calories and macros as a valid manual meal', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['patchMealFormModel']({
            manualCalories: 0,
            manualProteins: 0,
            manualFats: 0,
            manualCarbs: 0,
            manualFiber: 0,
            manualAlcohol: 0,
        });
        fixture.detectChanges();
        (fixture.nativeElement as HTMLElement)
            .querySelector('form')
            ?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        await fixture.whenStable();
        expect(component['caloriesError']()).toBeNull();
        expect(component['macrosError']()).toBeNull();
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
        expect(mealManageFacade.submitMealAsync.mock.calls[0][1]).toMatchObject({
            manualCalories: 0,
            manualProteins: 0,
            manualFats: 0,
            manualCarbs: 0,
            manualFiber: 0,
            manualAlcohol: 0,
        });
    });
});

describe('Meal form selected amount validation', () => {
    it.each([null, 0, -1, Number.NaN, Number.POSITIVE_INFINITY])('does not send an item with amount %s', async amount => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['patchMealFormModel']({
            items: [createMealItemValue({ ...createEmptyProductSnapshot(), id: entityId<'product'>('p') }, null, amount)],
        });
        fixture.detectChanges();
        await component['onSubmitAsync']();
        expect(component['itemListItems']()[0].amountError).not.toBeNull();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
    it('allows an unused empty placeholder alongside valid items', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['patchMealFormModel']({ items: [...component['items'], createMealItemValue()] });
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
    });
    it('shows general field errors on invalid native form submission', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['patchMealFormModel']({ date: '', time: '' });
        fixture.detectChanges();
        (fixture.nativeElement as HTMLElement)
            .querySelector('form')
            ?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        await fixture.whenStable();
        expect(component['globalError']()).toBe('FORM_ERRORS.CHECK_FIELDS');
        expect(component['generalFieldErrors']().date).not.toBeNull();
        expect(component['generalFieldErrors']().time).not.toBeNull();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

function setValidManualMeal(component: MealManageFormComponent): void {
    component['patchMealFormModel']({
        date: '2026-09-23',
        time: '01:00',
        mealType: 'Dinner',
        isNutritionAutoCalculated: false,
        manualCalories: TOTAL_CALORIES,
        manualProteins: PRODUCT_AMOUNT,
        manualFats: 0,
        manualCarbs: 0,
        manualFiber: 0,
        manualAlcohol: 0,
        items: [createMealItemValue({ ...createEmptyProductSnapshot(), id: entityId<'product'>('p') }, null, PRODUCT_AMOUNT)],
    });
}

describe('Meal form delayed item dialog', () => {
    it('does not overwrite another row when the edited item was removed while its dialog was open', async () => {
        const { component, fixture } = await setupComponentAsync();
        const original = createMealItemValue(
            { ...createEmptyProductSnapshot(), id: entityId<'product'>('original') },
            null,
            PRODUCT_AMOUNT,
        );
        const other = createMealItemValue({ ...createEmptyProductSnapshot(), id: entityId<'product'>('other') }, null, PRODUCT_AMOUNT);
        const selected = createMealItemValue(
            { ...createEmptyProductSnapshot(), id: entityId<'product'>('selected') },
            null,
            PRODUCT_AMOUNT,
        );
        component['patchMealFormModel']({ items: [original, other] });
        const response = new Subject<MealItemFormValues | null>();
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.spyOn(dialogs, 'open').mockReturnValue({ afterClosed: () => response } as unknown as ReturnType<typeof dialogs.open>);
        component['onItemSourceClick'](0);
        fixture.detectChanges();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]').click();
        response.next(selected);
        await waitForAsyncTasksAsync();
        expect(component['items']).toEqual([other]);
    });
});

describe('Meal form custom controls preserve unsaved changes', () => {
    it.each(['satiety', 'nutrition', 'remove', 'ai'] as const)('asks before discarding a %s change', async action => {
        const { component, fixture, mealManageFacade, navigationService } = await setupComponentAsync();
        mealManageFacade.confirmDiscardChangesAsync.mockResolvedValue(false);
        if (action === 'satiety') {
            component['onSatietyLevelChange']('postMealSatietyLevel', NORMALIZED_SATIETY_LEVEL);
        }
        if (action === 'nutrition') {
            component['onNutritionModeChange']('manual');
        }
        if (action === 'remove') {
            component['patchMealFormModel']({ items: [createCommentTestItem()] });
            fixture.detectChanges();
            requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]').click();
        }
        if (action === 'ai') {
            component['onAiMealRecognized']({
                source: 'Photo',
                imageAssetId: null,
                imageUrl: null,
                recognizedAtUtc: '2026-09-23T00:00:00Z',
                items: [],
            });
        }
        await component['onCancelAsync']();
        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToMealListAsync).not.toHaveBeenCalled();
    });
    it('asks before discarding an item accepted from the dialog', async () => {
        const { component, mealManageFacade, navigationService } = await setupComponentAsync();
        mealManageFacade.confirmDiscardChangesAsync.mockResolvedValue(false);
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.spyOn(dialogs, 'open').mockReturnValue({
            afterClosed: () =>
                of(createMealItemValue({ ...createEmptyProductSnapshot(), id: entityId<'product'>('p') }, null, PRODUCT_AMOUNT)),
        } as unknown as ReturnType<typeof dialogs.open>);
        component['onItemSourceClick'](0);
        await waitForAsyncTasksAsync();
        await component['onCancelAsync']();
        expect(mealManageFacade.confirmDiscardChangesAsync).toHaveBeenCalledOnce();
        expect(navigationService.navigateToMealListAsync).not.toHaveBeenCalled();
    });
});

describe('Meal form detached dialog results', () => {
    it('closes the manual editor and unsubscribes its result on destruction', async () => {
        const { component, fixture } = await setupComponentAsync();
        const response = new Subject<MealItemFormValues | null>();
        const close = vi.fn();
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.spyOn(dialogs, 'open').mockReturnValue({ afterClosed: () => response, close } as unknown as ReturnType<typeof dialogs.open>);
        component['onItemSourceClick'](0);
        fixture.destroy();
        await waitForAsyncTasksAsync();
        expect(close).toHaveBeenCalledExactlyOnceWith(null);
        expect(response.observed).toBe(false);
    });
    it('ignores a photo session editor result when its original session was removed', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const original = { notes: 'old', items: [] };
        const other = { notes: 'other', items: [] };
        component['aiSessions'].set([original, other]);
        let complete: ((value: MealAiSessionManageDto) => void) | undefined;
        mealManageFacade.openEditAiPhotoSessionDialogAsync.mockReturnValue(
            new Promise<MealAiSessionManageDto>(resolve => {
                complete = resolve;
            }),
        );
        fixture.detectChanges();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.ITEMS_AI_EDIT"]').click();
        requiredMealButton(fixture.nativeElement as HTMLElement, 'button[aria-label="MEAL_MANAGE.ITEMS_AI_DELETE"]').click();
        complete?.({ notes: 'edited', items: [] });
        await waitForAsyncTasksAsync();
        expect(mealManageFacade.replaceAiSession).not.toHaveBeenCalled();
        expect(component['aiSessions']()).toEqual([other]);
    });
});

describe('Meal form local date and time validation', () => {
    it('keeps the original instant when saving other fields without changing the local date or time', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        const original = new Date('2025-10-26T01:30:45.123Z');
        fixture.componentRef.setInput('meal', createMeal({ date: utcInstant(original.toISOString()) }));
        fixture.detectChanges();
        setValidManualMeal(component);
        component['patchMealFormModel']({
            date: getDateInputValue(original),
            time: getTimeInputValue(original),
            comment: 'Changed comment',
        });

        await component['onSubmitAsync']();

        expect(mealManageFacade.submitMealAsync.mock.calls[0][1]).toMatchObject({ date: original, comment: 'Changed comment' });
    });

    it('blocks an unavailable local date/time and revalidates when only the date changes', async () => {
        const { component, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        component['mealFormModel'].update(model => ({ ...model, date: '2026-02-30', time: '12:30' }));

        expect(component['mealSignalForm'].time().errors()).toEqual([expect.objectContaining({ kind: 'nonexistentLocalTime' })]);
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();

        component['mealFormModel'].update(model => ({ ...model, date: '2026-03-01' }));
        expect(component['mealSignalForm'].time().valid()).toBe(true);
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
    });
});

describe('Meal form save retry preserves draft state', () => {
    it('keeps dirty values after failure, retries the same payload and clears dirty state only after success', async () => {
        const { component, mealManageFacade, navigationService } = await setupComponentAsync();
        setValidManualMeal(component);
        component['onSatietyLevelChange']('preMealSatietyLevel', NORMALIZED_SATIETY_LEVEL);
        mealManageFacade.submitMealAsync
            .mockRejectedValueOnce(new HttpErrorResponse({ status: 500 }))
            .mockResolvedValueOnce(createMeal({ totalCalories: TOTAL_CALORIES }));
        const draft = component['mealFormModel']();
        await component['onSubmitAsync']();
        expect(component['mealFormModel']()).toEqual(draft);
        expect(component['mealSignalForm']().dirty()).toBe(true);
        expect(component['isSubmitting']()).toBe(false);
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
        await component['onSubmitAsync']();
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledTimes(2);
        expect(mealManageFacade.submitMealAsync.mock.calls[0]).toEqual(mealManageFacade.submitMealAsync.mock.calls[1]);
        expect(component['mealSignalForm']().dirty()).toBe(false);
        expect(component['globalError']()).toBeNull();
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).toHaveBeenCalledOnce();
        await component['onCancelAsync']();
        expect(mealManageFacade.confirmDiscardChangesAsync).not.toHaveBeenCalled();
        expect(navigationService.navigateToMealListAsync).toHaveBeenCalledOnce();
    });
});

it('does not redirect from a newer page when a pending save completes after the editor is destroyed', async () => {
    const { component, fixture, mealManageFacade } = await setupComponentAsync();
    setValidManualMeal(component);
    let finishSave: (value: Meal) => void = () => {};
    mealManageFacade.submitMealAsync.mockReturnValueOnce(
        new Promise<Meal>(resolve => {
            finishSave = resolve;
        }),
    );
    fixture.detectChanges();
    const form = (fixture.nativeElement as HTMLElement).querySelector('form');
    if (form === null) {
        throw new Error('Expected the rendered meal editor form');
    }
    form.requestSubmit();
    await vi.waitFor(() => {
        expect(mealManageFacade.submitMealAsync).toHaveBeenCalledOnce();
    });

    fixture.destroy();
    finishSave(createMeal());
    await waitForAsyncTasksAsync();

    expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
});

describe('Meal editor launch and item picker', () => {
    it('opens the meal-type bookmark and reports removal of its last item', async () => {
        const picker = new Subject<MealItemFormValues | null>();
        const { fixture, mealManageFacade, openDialog } = await setupComponentAsync(undefined, {
            query: { manual: '1', mealType: ' dinner ' },
            picker,
        });
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(openDialog).toHaveBeenCalledOnce();
        });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('fd-meal-general-info fd-ui-select')?.textContent).toContain('MEAL_TYPES.DINNER');

        const chosen = createCommentTestItem();
        picker.next(chosen);
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(mealManageFacade.configureItemType).toHaveBeenCalledExactlyOnceWith(chosen, MealSourceType.Product);
        expect(host.querySelector('.meal-manage__manual-summary')?.textContent).toContain('Apple');
        const save = requiredMealButton(host, 'button[type="submit"]');
        expect(save.disabled).toBe(false);

        requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]').click();
        fixture.detectChanges();

        expect(host.querySelector('.meal-manage__manual-summary')).toBeNull();
        expect(host.querySelector('.meal-item__error')?.textContent).toContain('FORM_ERRORS.NON_EMPTY_ARRAY');
        expect(save.disabled).toBe(true);
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });

    it('adds a distinct second item through the manual action and edits the visible list', async () => {
        const picker = new Subject<MealItemFormValues | null>();
        const { fixture, mealManageFacade, openDialog } = await setupComponentAsync(undefined, { query: {}, picker });
        fixture.componentRef.setInput(
            'meal',
            createMeal({
                items: [
                    {
                        id: entityId<'meal-item'>('original-item'),
                        mealId: entityId<'meal'>('meal-1'),
                        product: { ...createEmptyProductSnapshot(), id: entityId<'product'>('original-product'), name: 'Original food' },
                        recipe: null,
                        sourceType: MealSourceType.Product,
                        amount: productQuantityFromStored(PRODUCT_AMOUNT),
                    },
                ],
            }),
        );
        fixture.detectChanges();
        mealManageFacade.createMealItem.mockClear();
        const host = fixture.nativeElement as HTMLElement;
        const add = host.querySelector<HTMLButtonElement>('button[aria-label="AI_INPUT_BAR.MANUAL_ACTION"]');
        if (add === null) {
            throw new Error('Expected the rendered manual add action');
        }
        add.click();
        fixture.detectChanges();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(openDialog).toHaveBeenCalledOnce();
        });
        expect(mealManageFacade.createMealItem).toHaveBeenCalledOnce();

        picker.next(createCommentTestItem());
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        const names = (): string[] => Array.from(host.querySelectorAll('.meal-manage__ai-summary-name'), name => name.textContent.trim());
        expect(names()).toEqual(['Original food', 'Apple']);
        const remove = host.querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]');
        if (remove === null) {
            throw new Error('Expected the first item removal action');
        }
        remove.click();
        fixture.detectChanges();
        expect(names()).toEqual(['Apple']);
    });
});

describe('Meal editor displayed feedback', () => {
    it.each([false, true])('preserves the form when the server requires renewed consent, reload=%s', async needsReload => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        TestBed.inject(PrivacyConsentFacade).needsReload.set(needsReload);
        mealManageFacade.submitMealAsync.mockRejectedValueOnce(
            new HttpErrorResponse({ status: 403, error: { error: 'Privacy.HealthConsentRequired' } }),
        );
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const form = host.querySelector('form');
        if (form === null) {
            throw new Error('Expected the rendered meal editor form');
        }

        form.requestSubmit();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(host.querySelector('.meal-manage__footer-error')?.textContent).toContain(
            needsReload ? 'PRIVACY_CHOICES.RELOAD' : 'PRIVACY_CHOICES.ERROR',
        );
        expect(host.querySelector('.meal-manage__manual-summary')).not.toBeNull();
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
    });

    it('starts the tour only when the visible tour button is pressed', async () => {
        const { fixture } = await setupComponentAsync();
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        const host = fixture.nativeElement as HTMLElement;
        const button = host.querySelector<HTMLButtonElement>('[data-tour-id="meal-manage-tour-help"] button');
        if (button === null) {
            throw new Error('Expected the rendered meal tour button');
        }
        expect(start).not.toHaveBeenCalled();

        button.click();

        expect(start).toHaveBeenCalledExactlyOnceWith(expect.any(Object), { force: true });
    });
});

describe('Meal editor clears a recognized draft', () => {
    it('removes visible recognition, calories and the ability to save when a development draft is cleared', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const { fixture, mealManageFacade } = await setupComponentAsync({
            attach: screen => {
                screens.push(screen);
                return (): void => {};
            },
        });
        mealManageFacade.buildNutritionSummaryStateFromValues.mockImplementation((_form, sessions: MealAiSessionManageDto[]) =>
            createNutritionSummaryStateWithCalories(sessions.length > 0 ? TOTAL_CALORIES : 0),
        );
        renderRecognizedMeal(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const save = requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.ADD_BUTTON"]');
        expect(host.querySelector('fd-meal-ai-sessions')?.textContent).toContain('Grilled salmon');
        expect(screens[0].read()).toMatchObject({ calories: TOTAL_CALORIES, dirty: true });
        expect(save.disabled).toBe(false);

        screens[0].clearDraft?.();
        fixture.detectChanges();

        expect(host.querySelector('fd-meal-ai-sessions')).toBeNull();
        expect(screens[0].read()).toMatchObject({ calories: 0, dirty: false, valid: false, items: [] });
        expect(save.disabled).toBe(true);
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

describe('Meal editor consent ownership', () => {
    it('does not send a previously displayed meal after approval when the route editor has been reused', async () => {
        const { fixture, mealManageFacade } = await setupComponentAsync();
        const original = mealWithProduct('pending-consent', 'The original meal');
        const next = mealWithProduct('next-meal', 'A different meal');
        let approve: ((allowed: boolean) => void) | undefined;
        const consent = vi.spyOn(TestBed.inject(PrivacyConsentFacade), 'ensureHealthConsentAsync').mockReturnValueOnce(
            new Promise(resolve => {
                approve = resolve;
            }),
        );
        fixture.componentRef.setInput('meal', original);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const save = requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.SAVE_BUTTON"]');
        save.click();
        await vi.waitFor(() => {
            expect(consent).toHaveBeenCalledTimes(1);
        });
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();

        fixture.componentRef.setInput('meal', next);
        fixture.detectChanges();
        approve?.(true);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('form')?.getAttribute('aria-busy')).toBe('false');
        });

        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
        expect(host.querySelector('textarea')?.value).toBe(next.comment);
        expect(requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.SAVE_BUTTON"]').disabled).toBe(false);
    });

    it('does not write a meal when the editor loses ownership during the consent review', async () => {
        const { component, fixture, mealManageFacade } = await setupComponentAsync();
        setValidManualMeal(component);
        let allow: ((value: boolean) => void) | undefined;
        const privacy = TestBed.inject(PrivacyConsentFacade);
        const review = vi.spyOn(privacy, 'ensureHealthConsentAsync').mockReturnValueOnce(
            new Promise(resolve => {
                allow = resolve;
            }),
        );
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.ADD_BUTTON"]').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(review).toHaveBeenCalledExactlyOnceWith(true, {
                acceptLabelKey: 'PRIVACY_CHOICES.ACCEPT_AND_SAVE',
            });
            expect(host.querySelector('form')?.getAttribute('aria-busy')).toBe('true');
        });
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        fixture.destroy();
        allow?.(true);
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
        expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
    });
});

describe('Meal editor late save responses', () => {
    it.each(['success', 'error'] as const)('keeps the current meal visible when a previous save returns %s', async result => {
        const { fixture, mealManageFacade } = await setupComponentAsync();
        const current = mealWithProduct('saving-meal', 'Save this note');
        const refreshed = mealWithProduct('other-meal', 'Keep this newly loaded note');
        let finish: (() => void) | undefined;
        mealManageFacade.submitMealAsync.mockReturnValueOnce(
            new Promise<Meal>((resolve, reject) => {
                finish = (): void => {
                    if (result === 'success') {
                        resolve(current);
                    } else {
                        reject(new Error('Error from the previous meal'));
                    }
                };
            }),
        );
        fixture.componentRef.setInput('meal', current);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const save = requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.SAVE_BUTTON"]');
        save.click();
        await vi.waitFor(() => {
            expect(mealManageFacade.submitMealAsync).toHaveBeenCalledExactlyOnceWith(
                current,
                expect.objectContaining({ comment: current.comment }),
            );
        });

        fixture.componentRef.setInput('meal', refreshed);
        fixture.detectChanges();
        finish?.();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('form')?.getAttribute('aria-busy')).toBe('false');
        });

        expect(mealManageFacade.showSuccessToastAndRedirectAsync).not.toHaveBeenCalled();
        expect(host.querySelector('textarea')?.value).toBe(refreshed.comment);
        expect(host.querySelector('.meal-manage__footer-error')).toBeNull();
        expect(requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.SAVE_BUTTON"]').disabled).toBe(false);
    });
});

describe('Meal editor development capability during photo preparation', () => {
    it('preserves a recognized draft until the pending photo settles', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const { fixture, mealManageFacade } = await setupComponentAsync(captureMealDevelopment(screens));
        renderRecognizedMeal(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const control = mealPhotoControl(fixture);
        const clearDraft = screens[0].clearDraft;
        expect(clearDraft).toBeDefined();

        control.imagePreparationStarted.emit('blob:meal-draft-photo');
        fixture.detectChanges();
        expect(requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.ADD_BUTTON"]').disabled).toBe(true);
        expect(() => clearDraft?.()).toThrow('A ready new meal editor is required.');
        expect(host.querySelector('fd-meal-ai-sessions')?.textContent).toContain('Grilled salmon');

        control.imagePreparationFailed.emit();
        fixture.detectChanges();
        clearDraft?.();
        fixture.detectChanges();

        expect(host.querySelector('fd-meal-ai-sessions')).toBeNull();
        expect(screens[0].read()).toMatchObject({ dirty: false, valid: false });
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });

    it('does not apply a pending development draft after photo preparation starts', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const { fixture, mealManageFacade } = await setupComponentAsync(captureMealDevelopment(screens));
        let resolveAmount: ((item: CommittedMealItemFormValues) => void) | undefined;
        mealManageFacade.prepareRecipeItemAsync.mockReturnValueOnce(
            new Promise(resolve => {
                resolveAmount = resolve;
            }),
        );
        const control = mealPhotoControl(fixture);
        const draft = {
            date: '2026-10-17',
            time: '12:00',
            mealType: 'LUNCH',
            comment: 'Imported serving',
            items: [{ recipe: createEmptyRecipeSnapshot(), servings: recipeServingsFromStored(FRACTIONAL_RECIPE_SERVINGS) }],
        };
        const pending = screens[0].fillDraft?.(draft, vi.fn());
        expect(mealManageFacade.prepareRecipeItemAsync).toHaveBeenCalledTimes(1);
        control.imagePreparationStarted.emit('blob:meal-draft-photo');
        fixture.detectChanges();
        resolveAmount?.(createRecipeEditorItem(draft.items[0].recipe, PRODUCT_AMOUNT));

        await expect(pending).rejects.toThrow('Meal editor changed while resolving serving quantities.');
        await expect(screens[0].fillDraft?.(draft, vi.fn())).rejects.toThrow('A clean new meal editor is required.');
        expect(screens[0].read()).toMatchObject({ items: [], dirty: false });
        expect(mealManageFacade.prepareRecipeItemAsync).toHaveBeenCalledTimes(1);
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

describe('Meal editor visible language and development projection', () => {
    it('updates the selected meal type label when switching the visible language', async () => {
        const { fixture, mealManageFacade } = await setupComponentAsync();
        const translations = TestBed.inject(TranslateService);
        translations.setTranslation('en', { MEAL_TYPES: { LUNCH: 'Lunch' } });
        translations.setTranslation('ru', { MEAL_TYPES: { LUNCH: 'Обед' } });
        await firstValueFrom(translations.use('en'));
        const meal = { ...mealWithProduct('local-meal', 'Unchanged note'), mealType: 'Lunch' };
        fixture.componentRef.setInput('meal', meal);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const select = requiredMealButton(host, 'fd-meal-general-info button[aria-haspopup="listbox"]');
        expect(select.textContent).toContain('Lunch');

        await firstValueFrom(translations.use('ru'));
        fixture.detectChanges();

        expect(select.textContent).toContain('Обед');
        expect(host.querySelector('textarea')?.value).toBe(meal.comment);
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });

    it('reports product and recipe quantities after the user cancels adding an empty item', async () => {
        const screens: MealDevelopmentScreen[] = [];
        const { fixture, mealManageFacade, openDialog } = await setupComponentAsync(captureMealDevelopment(screens));
        const meal = mealWithProduct('loaded-ingredients', 'Meal with recipe and product');
        const recipe = { ...createEmptyRecipeSnapshot(), id: entityId<'recipe'>('soup'), name: 'Soup' };
        mealManageFacade.prepareMealItems.mockReturnValueOnce(
            of([createMealItemValue(meal.items[0].product, null, PRODUCT_AMOUNT), createRecipeEditorItem(recipe, PRODUCT_AMOUNT)]),
        );
        fixture.componentRef.setInput('meal', {
            ...meal,
            items: [
                ...meal.items,
                {
                    id: entityId<'meal-item'>('soup-item'),
                    mealId: meal.id,
                    sourceType: MealSourceType.Recipe,
                    amount: recipeServingsFromStored(FRACTIONAL_RECIPE_SERVINGS),
                    recipe,
                    product: null,
                },
            ],
        });
        fixture.detectChanges();

        const picker = new Subject<MealItemFormValues | null>();
        openDialog.mockReturnValueOnce({ afterClosed: () => picker, close: vi.fn() });
        const host = fixture.nativeElement as HTMLElement;
        requiredMealButton(host, 'button[aria-label="AI_INPUT_BAR.MANUAL_ACTION"]').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(openDialog.mock.lastCall?.[1]).toMatchObject({ data: { item: { product: null, recipe: null } } });
        });
        picker.next(null);
        picker.complete();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();

        expect(screens[0].read()).toMatchObject({
            kind: 'edit-meal-editor',
            items: [
                { name: 'Apple', productId: 'apple', recipeId: null, servings: null, amount: PRODUCT_AMOUNT },
                {
                    name: 'Soup',
                    productId: null,
                    recipeId: recipe.id,
                    servings: FRACTIONAL_RECIPE_SERVINGS,
                    amount: PRODUCT_AMOUNT,
                    unit: 'grams',
                },
            ],
        });
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

describe('Meal editor closes a recognition review', () => {
    it('keeps a recognized dish when the edit is cancelled and ignores a later result after leaving', async () => {
        const { fixture, mealManageFacade } = await setupComponentAsync();
        renderRecognizedMeal(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const edit = requiredMealButton(host, 'button[aria-label="MEAL_MANAGE.ITEMS_AI_EDIT"]');
        edit.click();
        await waitForAsyncTasksAsync();
        fixture.detectChanges();
        expect(host.querySelector('fd-meal-ai-sessions')?.textContent).toContain('Grilled salmon');
        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).toHaveBeenCalledOnce();
        expect(mealManageFacade.replaceAiSession).not.toHaveBeenCalled();

        let finish: ((value: MealAiSessionManageDto) => void) | undefined;
        mealManageFacade.openEditAiPhotoSessionDialogAsync.mockReturnValueOnce(
            new Promise(resolve => {
                finish = resolve;
            }),
        );
        edit.click();
        expect(mealManageFacade.openEditAiPhotoSessionDialogAsync).toHaveBeenCalledTimes(2);
        fixture.destroy();
        finish?.({ notes: 'Late recognition edit', items: [] });
        await waitForAsyncTasksAsync();

        expect(mealManageFacade.replaceAiSession).not.toHaveBeenCalled();
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

describe('Meal editor rendered nutrition modes', () => {
    it('copies calculated values to the manual fields and restores the automatic values after switching back', async () => {
        const { fixture, mealManageFacade } = await setupComponentAsync();
        const totals: NutritionTotals = { calories: TOTAL_CALORIES, proteins: 25, fats: 18, carbs: 10, fiber: 4, alcohol: 1 };
        mealManageFacade.buildNutritionSummaryStateFromValues.mockReturnValue({
            autoTotals: totals,
            summaryTotals: totals,
            warning: null,
        });
        renderRecognizedMeal(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const options = Array.from(host.querySelectorAll<HTMLButtonElement>('.meal-manage__nutrition-toggle button'));
        const [automatic, manual] = options;
        const calories = requiredMealInput(host, '.nutrition-editor__input--calories input');
        const fiber = requiredMealInput(host, '.nutrition-editor__input--fiber input');
        const alcohol = requiredMealInput(host, '.nutrition-editor__input--alcohol input');
        expect(automatic.getAttribute('aria-checked')).toBe('true');
        expect(calories.readOnly).toBe(true);

        manual.click();
        fixture.detectChanges();
        expect(manual.getAttribute('aria-checked')).toBe('true');
        expect(calories.readOnly).toBe(false);
        expect([calories.value, fiber.value, alcohol.value]).toEqual([
            String(totals.calories),
            String(totals.fiber),
            String(totals.alcohol),
        ]);
        calories.value = '280';
        calories.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(calories.value).toBe('280');

        manual.click();
        fixture.detectChanges();
        expect(calories.value).toBe('280');

        automatic.click();
        fixture.detectChanges();
        expect(calories.readOnly).toBe(true);
        expect(calories.value).toBe(String(totals.calories));
        expect(mealManageFacade.submitMealAsync).not.toHaveBeenCalled();
    });
});

function renderRecognizedMeal(fixture: ComponentFixture<MealManageFormComponent>): void {
    const section = fixture.debugElement.query(By.directive(MealItemsSectionComponent)).componentInstance as MealItemsSectionComponent;
    section.mealRecognized.emit({
        source: 'Photo',
        imageUrl: 'https://images.example.test/salmon.jpg',
        recognizedAtUtc: '2026-05-21T12:30:00Z',
        items: [
            {
                nameEn: 'Grilled salmon',
                amount: PRODUCT_AMOUNT,
                unit: 'g',
                calories: TOTAL_CALORIES,
                proteins: 25,
                fats: 18,
                carbs: 0,
                fiber: 0,
                alcohol: 0,
            },
        ],
    });
    fixture.detectChanges();
}

function mealWithProduct(id: string, comment: string): Meal {
    const mealId = entityId<'meal'>(id);
    return createMeal({
        id: mealId,
        comment,
        items: [
            {
                id: entityId<'meal-item'>('apple-item'),
                mealId,
                amount: productQuantityFromStored(PRODUCT_AMOUNT),
                sourceType: MealSourceType.Product,
                product: { ...createEmptyProductSnapshot(), id: entityId<'product'>('apple'), name: 'Apple' },
                recipe: null,
            },
        ],
    });
}

function captureMealDevelopment(screens: MealDevelopmentScreen[]): MealDevelopmentTools {
    return {
        attach: screen => {
            screens.push(screen);
            return (): void => undefined;
        },
    };
}

function mealPhotoControl(fixture: ComponentFixture<MealManageFormComponent>): ImageUploadFieldComponent {
    return fixture.debugElement.query(By.directive(MealGeneralInfoComponent)).query(By.directive(ImageUploadFieldComponent))
        .componentInstance as ImageUploadFieldComponent;
}

function requiredMealButton(host: HTMLElement, selector: string): HTMLButtonElement {
    const button = host.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected meal editor button ${selector}`);
    }
    return button;
}

function requiredMealInput(host: HTMLElement, selector: string): HTMLInputElement {
    const input = host.querySelector(selector);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Expected meal editor input ${selector}`);
    }
    return input;
}
