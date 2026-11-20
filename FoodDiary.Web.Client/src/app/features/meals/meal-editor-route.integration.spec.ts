import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { FoodDiaryTranslationLoader } from '../../shared/i18n/food-diary-translation.loader';
import { createEmptyProductSnapshot, type Meal, type MealOverview, MealSourceType } from '../../shared/models/meal.data';
import type { Product } from '../../shared/models/product.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { MAXIMUM_CONSUMED_QUANTITY, productQuantityFromStored } from '../../shared/models/semantics/meal-quantity';
import { ViewportService } from '../../shared/platform/viewport.service';
import { PrivacyConsentFacade } from '../../shared/privacy/privacy-consent.facade';
import { MealService } from './api/meal.service';
import mealsRoutes from './meals.routes';

const MEALS_URL = '/meals';
const ADD_URL = '/meals/add';
const OTHER_URL = '/home';
const PRIVACY_URL = '/privacy-settings';
const MEAL_ID = entityId<'meal'>('saved-dinner');
const MEAL_URL = `/meals/${MEAL_ID}/edit`;
const PRODUCT_ID = entityId<'product'>('apple');
const PRODUCT_AMOUNT = 150;
const CORRECTED_AMOUNT = 212.5;
const LAZY_ROUTE_TIMEOUT_MS = 5000;
const ORIGINAL_NOTE = 'Dinner saved yesterday';
const DIARY: MealOverview = {
    allMeals: { data: [], page: 1, limit: 10, totalPages: 1, totalItems: 0 },
    favoriteItems: [],
    favoriteTotalCount: 0,
};

@Component({ selector: 'fd-meal-editor-test-home', template: '<h1>Home</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class MealEditorHomeComponent {}

@Component({
    selector: 'fd-meal-editor-test-privacy',
    template: '<h1>Privacy preferences</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class MealEditorPrivacyComponent {}

type MealEditorApi = {
    [Method in 'getById' | 'queryOverview' | 'create' | 'update']: Mock<MealService[Method]>;
};

function setupEditorRoutes({ consentGranted = true, premium = false } = {}): MealEditorApi {
    const api: MealEditorApi = {
        getById: vi.fn<MealService['getById']>().mockReturnValue(of(savedMeal())),
        queryOverview: vi.fn<MealService['queryOverview']>().mockReturnValue(of(DIARY)),
        create: vi.fn<MealService['create']>(),
        update: vi.fn<MealService['update']>(),
    };
    vi.stubGlobal('matchMedia', (media: string) => ({ matches: false, media }));
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [
                    { path: 'meals', children: mealsRoutes },
                    { path: 'home', component: MealEditorHomeComponent },
                    { path: 'privacy-settings', component: MealEditorPrivacyComponent },
                ],
                withComponentInputBinding(),
            ),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: FoodDiaryTranslationLoader,
                useValue: {
                    isPublicRoute: vi.fn<FoodDiaryTranslationLoader['isPublicRoute']>().mockReturnValue(false),
                    loadApplicationTranslations: vi.fn<FoodDiaryTranslationLoader['loadApplicationTranslations']>().mockReturnValue(of({})),
                },
            },
            { provide: MealService, useValue: api },
            { provide: AuthService, useValue: { isAuthenticated: signal(true), isPremium: signal(premium), getUserId: (): null => null } },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            {
                provide: PrivacyConsentFacade,
                useValue: {
                    ensureHealthConsentAsync: vi.fn<PrivacyConsentFacade['ensureHealthConsentAsync']>().mockResolvedValue(consentGranted),
                    failed: signal(false),
                    needsReload: signal(false),
                },
            },
            { provide: FdUiToastService, useValue: { success: vi.fn(), error: vi.fn() } },
        ],
    });
    const dialogs = TestBed.inject(FdUiDialogService);
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        http.verify();
        dialogs.closeAll();
        vi.unstubAllGlobals();
    });
    return api;
}

function savedMeal(comment = ORIGINAL_NOTE, amount = PRODUCT_AMOUNT, product?: Product): Meal {
    return {
        id: MEAL_ID,
        date: utcInstant('2026-05-04T15:00:00Z'),
        mealType: 'Dinner',
        comment,
        totalCalories: 80,
        totalProteins: 1,
        totalFats: 0,
        totalCarbs: 20,
        totalFiber: 3,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [
            {
                id: entityId<'meal-item'>('apple-portion'),
                mealId: MEAL_ID,
                amount: productQuantityFromStored(amount),
                sourceType: MealSourceType.Product,
                product: product ?? { ...createEmptyProductSnapshot(), id: PRODUCT_ID, name: 'Green apple' },
                recipe: null,
            },
        ],
        aiSessions: [],
    };
}

describe('Routed meal editor inline hunger and satiety', () => {
    it('updates hunger and clears an existing satiety answer through the inline controls', async () => {
        const before = 2;
        const after = 3;
        const changedBefore = 4;
        const meal: Meal = { ...savedMeal(), preMealSatietyLevel: before, postMealSatietyLevel: after };
        const api = setupEditorRoutes();
        api.getById.mockReturnValueOnce(of(meal));
        api.update.mockReturnValueOnce(of({ ...meal, preMealSatietyLevel: changedBefore, postMealSatietyLevel: null }));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const beforeLabel = 'HUNGER_BEFORE_SCALE.LEVEL_2.TITLE';
        const afterLabel = 'SATIETY_AFTER_SCALE.LEVEL_3.TITLE';
        const changedBeforeLabel = 'HUNGER_BEFORE_SCALE.LEVEL_4.TITLE';
        expect(satietyOption(editor, beforeLabel).getAttribute('aria-checked')).toBe('true');
        expect(satietyOption(editor, afterLabel).getAttribute('aria-checked')).toBe('true');

        satietyOption(editor, changedBeforeLabel).click();
        nativeEditorButton(editor, 'MEAL_DETAILS.CLEAR_SATIETY').click();
        harness.detectChanges();
        expect(satietyOption(editor, changedBeforeLabel).getAttribute('aria-checked')).toBe('true');
        expect(satietyOption(editor, afterLabel).getAttribute('aria-checked')).toBe('false');

        nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await waitForDiaryAsync(harness);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({
                preMealSatietyLevel: changedBefore,
                postMealSatietyLevel: null,
                items: [expect.objectContaining({ productId: PRODUCT_ID, amount: PRODUCT_AMOUNT })],
            }),
        );
    });
});

describe('Production lazy meal editors', () => {
    it('passes the resolved meal into the edit form and saves the native change on that meal', async () => {
        const api = setupEditorRoutes();
        const update = new Subject<Meal>();
        api.update.mockReturnValueOnce(update);
        const harness = await RouterTestingHarness.create(MEAL_URL);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(MEAL_ID);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const note = 'Updated only the dinner note';
        const comment = commentInput(editor);
        expect(comment.value).toBe(ORIGINAL_NOTE);
        expect(editor.textContent).toContain('Green apple');
        changeComment(comment, note);
        harness.detectChanges();
        const save = nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON');
        expect(save.disabled).toBe(false);
        save.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.update).toHaveBeenCalledExactlyOnceWith(
                MEAL_ID,
                expect.objectContaining({
                    comment: note,
                    items: [expect.objectContaining({ productId: PRODUCT_ID, amount: PRODUCT_AMOUNT })],
                }),
            );
        });
        expect(TestBed.inject(Router).url).toBe(MEAL_URL);
        expect(api.create).not.toHaveBeenCalled();

        update.next(savedMeal(note));
        update.complete();
        await waitForDiaryAsync(harness);
    });

    it('keeps the native add draft on Stay and leaves only when the user discards', async () => {
        const api = setupEditorRoutes();
        const harness = await RouterTestingHarness.create(ADD_URL);
        const editor = requireEditor(harness, 'fd-meal-add');
        const comment = commentInput(editor);
        const router = TestBed.inject(Router);
        expect(comment.value).toBe('');
        changeComment(comment, 'Keep this new note until confirmed');
        harness.detectChanges();

        const stay = router.navigateByUrl(OTHER_URL);
        await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.STAY');
        await expect(stay).resolves.toBe(false);
        expect(router.url).toBe(ADD_URL);
        expect(comment.value).toBe('Keep this new note until confirmed');

        const discard = router.navigateByUrl(OTHER_URL);
        await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.DISCARD');
        await expect(discard).resolves.toBe(true);
        expect(router.url).toBe(OTHER_URL);
        expect(harness.routeNativeElement?.textContent).toContain('Home');
        expect(api.getById).not.toHaveBeenCalled();
        expect(api.create).not.toHaveBeenCalled();
    });
});

describe('Production meal edit route fallback', () => {
    it.each(['blank', 'missing', 'failed'] as const)('returns to the diary for a %s meal', async reason => {
        const api = setupEditorRoutes();
        api.getById.mockReturnValueOnce(reason === 'failed' ? throwError(() => new Error('Unavailable')) : of(null));
        const harness = await RouterTestingHarness.create(`/meals/${reason === 'blank' ? '%20' : MEAL_ID}/edit`);

        await waitForDiaryAsync(harness);
        expect(harness.routeNativeElement?.querySelector('fd-meal-edit')).toBeNull();
        expect(api.getById.mock.calls).toEqual(reason === 'blank' ? [] : [[MEAL_ID]]);
        expect(api.update).not.toHaveBeenCalled();
    });

    it('opens the privacy destination before reading the edit meal when health consent is denied', async () => {
        const api = setupEditorRoutes({ consentGranted: false });
        const harness = await RouterTestingHarness.create(MEAL_URL);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(TestBed.inject(Router).url).toBe(PRIVACY_URL);
            expect(harness.routeNativeElement?.textContent).toContain('Privacy preferences');
        });
        expect(api.getById).not.toHaveBeenCalled();
        expect(api.update).not.toHaveBeenCalled();
    });
});

describe('Production saved meal quantity and update recovery', () => {
    it('blocks an excessive quantity, preserves the corrected draft after a failed write and retries the original meal', async () => {
        const api = setupEditorRoutes();
        const writing = new Subject<Meal>();
        api.update.mockReturnValueOnce(writing).mockReturnValueOnce(of(savedMeal(ORIGINAL_NOTE, CORRECTED_AMOUNT)));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const item = openMealQuantityEditor(harness, editor);
        const amount = mealQuantityInput(item);
        const applyAmount = nativeEditorButton(item, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE');
        expect(amount.valueAsNumber).toBe(PRODUCT_AMOUNT);

        enterMealQuantity(amount, MAXIMUM_CONSUMED_QUANTITY + 1);
        harness.detectChanges();
        expect(item.textContent).toContain('MEAL_MANAGE.MAX_AMOUNT_ERROR');
        expect(amount.getAttribute('aria-invalid')).toBe('true');
        expect(applyAmount.disabled).toBe(true);
        applyAmount.click();
        expect(item.isConnected).toBe(true);
        expect(api.update).not.toHaveBeenCalled();

        enterMealQuantity(amount, CORRECTED_AMOUNT);
        harness.detectChanges();
        expect(applyAmount.disabled).toBe(false);
        applyAmount.click();
        await vi.waitFor(() => {
            expect(item.isConnected).toBe(false);
        });
        harness.detectChanges();

        const save = nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON');
        save.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.update).toHaveBeenCalledExactlyOnceWith(
                MEAL_ID,
                expect.objectContaining({ items: [expect.objectContaining({ productId: PRODUCT_ID, amount: CORRECTED_AMOUNT })] }),
            );
            expect(save.disabled).toBe(true);
        });
        await expect(TestBed.inject(Router).navigateByUrl(OTHER_URL)).resolves.toBe(false);
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();

        writing.error(new HttpErrorResponse({ status: 0 }));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(editor.querySelector('.meal-manage__footer-error')?.textContent).toContain('FORM_ERRORS.SAVE_CONNECTION');
            expect(save.disabled).toBe(false);
        });
        const retained = openMealQuantityEditor(harness, editor);
        expect(mealQuantityInput(retained).valueAsNumber).toBe(CORRECTED_AMOUNT);
        nativeEditorButton(retained, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            expect(retained.isConnected).toBe(false);
        });
        harness.detectChanges();

        save.click();
        await waitForDiaryAsync(harness);
        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update.mock.calls[1]).toEqual(api.update.mock.calls[0]);
        expect(api.create).not.toHaveBeenCalled();
    });
});

describe('Meal quantity dialog route ownership', () => {
    it.each(['clean', 'edited'] as const)('closes an unapplied quantity when leaving the %s meal form', async draft => {
        const api = setupEditorRoutes();
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const router = TestBed.inject(Router);
        if (draft === 'edited') {
            changeComment(commentInput(editor), 'A note that has not been saved');
            harness.detectChanges();
        }
        const itemDialog = openMealQuantityEditor(harness, editor);
        const amount = mealQuantityInput(itemDialog);
        expect(amount.valueAsNumber).toBe(PRODUCT_AMOUNT);
        enterMealQuantity(amount, CORRECTED_AMOUNT);
        harness.detectChanges();
        expect(amount.valueAsNumber).toBe(CORRECTED_AMOUNT);

        if (draft === 'edited') {
            const stay = router.navigateByUrl(OTHER_URL);
            await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.STAY');
            await expect(stay).resolves.toBe(false);
            expect(itemDialog.isConnected).toBe(true);
            expect(amount.valueAsNumber).toBe(CORRECTED_AMOUNT);
        }
        const departure = router.navigateByUrl(OTHER_URL);
        if (draft === 'edited') {
            await chooseDepartureAsync(harness, 'UNSAVED_CHANGES.DISCARD');
        }
        await expect(departure).resolves.toBe(true);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Home');
        expect(itemDialog.isConnected).toBe(false);
        expect(api.update).not.toHaveBeenCalled();

        await harness.navigateByUrl(MEAL_URL);
        const reopened = openMealQuantityEditor(harness, requireEditor(harness, 'fd-meal-edit'));
        expect(mealQuantityInput(reopened).valueAsNumber).toBe(PRODUCT_AMOUNT);
    });
});

describe('Meal quantity ownership when the edit route is reused', () => {
    it('dismisses the original quantity draft and saves only the new meal', async () => {
        const api = setupEditorRoutes();
        const nextMealId = entityId<'meal'>('next-meal');
        const nextProductId = entityId<'product'>('banana');
        const nextAmount = 80;
        const nextNote = 'Another saved meal';
        const nextProduct = { ...createEmptyProductSnapshot(), id: nextProductId, name: 'Banana' };
        const nextSnapshot = savedMeal(nextNote, nextAmount, nextProduct);
        const nextMeal: Meal = {
            ...nextSnapshot,
            id: nextMealId,
            items: nextSnapshot.items.map(item => ({ ...item, id: entityId<'meal-item'>('banana-portion'), mealId: nextMealId })),
        };
        api.getById.mockReturnValueOnce(of(savedMeal())).mockReturnValueOnce(of(nextMeal));
        api.update.mockReturnValueOnce(
            of({
                ...nextMeal,
                items: nextMeal.items.map(item =>
                    item.sourceType === MealSourceType.Product ? { ...item, amount: productQuantityFromStored(CORRECTED_AMOUNT) } : item,
                ),
            }),
        );
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const originalEditor = requireEditor(harness, 'fd-meal-edit');
        const originalDialog = openMealQuantityEditor(harness, originalEditor);
        expect(originalDialog.textContent).toContain('Green apple');
        enterMealQuantity(mealQuantityInput(originalDialog), CORRECTED_AMOUNT);
        harness.detectChanges();

        await harness.navigateByUrl(`/meals/${nextMealId}/edit`);
        harness.detectChanges();
        const nextEditor = requireEditor(harness, 'fd-meal-edit');
        expect(nextEditor).toBe(originalEditor);
        expect(commentInput(nextEditor).value).toBe(nextNote);
        expect(originalDialog.isConnected).toBe(false);
        expect(api.update).not.toHaveBeenCalled();

        const nextDialog = openMealQuantityEditor(harness, nextEditor);
        expect(nextDialog.textContent).toContain('Banana');
        const quantity = mealQuantityInput(nextDialog);
        expect(quantity.valueAsNumber).toBe(nextAmount);
        enterMealQuantity(quantity, CORRECTED_AMOUNT);
        harness.detectChanges();
        nativeEditorButton(nextDialog, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(nextDialog.isConnected).toBe(false);
        });
        nativeEditorButton(nextEditor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await waitForDiaryAsync(harness);
        expect(api.getById.mock.calls).toEqual([[MEAL_ID], [nextMealId]]);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            nextMealId,
            expect.objectContaining({
                comment: nextNote,
                items: [{ productId: nextProductId, recipeId: null, amount: CORRECTED_AMOUNT, origin: 'Manual' }],
            }),
        );
    });
});

describe('Meal recognition review route ownership', () => {
    it('closes an open recognition review when leaving the meal and keeps the saved items unchanged', async () => {
        const api = setupEditorRoutes({ premium: true });
        api.getById.mockReturnValue(of(mealWithRecognition(MEAL_ID, ORIGINAL_NOTE, 'Broccoli')));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        nativeEditorButton(editor, 'MEAL_MANAGE.ITEMS_AI_EDIT').click();
        const review = await waitForRecognitionReviewAsync(harness, 'Broccoli');

        await harness.navigateByUrl(OTHER_URL);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Home');
        expect(review.isConnected).toBe(false);
        expect(api.update).not.toHaveBeenCalled();

        await harness.navigateByUrl(MEAL_URL);
        expect(requireEditor(harness, 'fd-meal-edit').querySelector('fd-meal-ai-sessions')?.textContent).toContain('Broccoli');
    });

    it('dismisses the original recognition review when the editor opens another meal on the same route', async () => {
        const api = setupEditorRoutes({ premium: true });
        const nextMealId = entityId<'meal'>('next-dinner');
        const nextNote = 'Next dinner';
        api.getById
            .mockReturnValueOnce(of(mealWithRecognition(MEAL_ID, ORIGINAL_NOTE, 'Broccoli')))
            .mockReturnValueOnce(of(mealWithRecognition(nextMealId, nextNote, 'Cauliflower')));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const originalEditor = requireEditor(harness, 'fd-meal-manage-form');
        nativeEditorButton(originalEditor, 'MEAL_MANAGE.ITEMS_AI_EDIT').click();
        const originalReview = await waitForRecognitionReviewAsync(harness, 'Broccoli');

        await harness.navigateByUrl(`/meals/${nextMealId}/edit`);
        harness.detectChanges();
        const nextEditor = requireEditor(harness, 'fd-meal-manage-form');
        expect(nextEditor).toBe(originalEditor);
        expect(commentInput(nextEditor).value).toBe(nextNote);
        expect(originalReview.isConnected).toBe(false);

        nativeEditorButton(nextEditor, 'MEAL_MANAGE.ITEMS_AI_EDIT').click();
        const nextReview = await waitForRecognitionReviewAsync(harness, 'Cauliflower');
        nativeEditorButton(nextReview, 'COMMON.CANCEL').click();
        await vi.waitFor(() => {
            expect(nextReview.isConnected).toBe(false);
        });
        expect(api.getById.mock.calls).toEqual([[MEAL_ID], [nextMealId]]);
        expect(api.update).not.toHaveBeenCalled();
    });
});

function mealWithRecognition(mealId: Meal['id'], comment: string, food: string): Meal {
    const meal = savedMeal(comment);
    const sessionId = `${mealId}-recognition`;
    return {
        ...meal,
        id: mealId,
        items: meal.items.map(item => ({ ...item, id: entityId<'meal-item'>(`${mealId}-portion`), mealId })),
        aiSessions: [
            {
                id: sessionId,
                mealId,
                imageUrl: `/images/${mealId}.jpg`,
                recognizedAtUtc: '2026-05-04T15:00:00Z',
                items: [
                    {
                        id: `${sessionId}-item`,
                        sessionId,
                        nameEn: food,
                        amount: 100,
                        unit: 'g',
                        calories: 34,
                        proteins: 3,
                        fats: 0,
                        carbs: 7,
                        fiber: 3,
                        alcohol: 0,
                    },
                ],
            },
        ],
    };
}

async function waitForRecognitionReviewAsync(harness: RouterTestingHarness, food: string): Promise<Element> {
    return vi.waitFor(() => {
        harness.detectChanges();
        const dialog = document.querySelector('fd-meal-photo-recognition-dialog');
        if (dialog === null) {
            throw new Error('Expected the native meal recognition review');
        }
        expect(dialog.textContent).toContain(food);
        return dialog;
    });
}

describe('Routed meal manual item cancellation', () => {
    it('keeps the saved meal editable after cancelling the empty item picker twice', async () => {
        const api = setupEditorRoutes();
        api.update.mockReturnValueOnce(of(savedMeal(ORIGINAL_NOTE, CORRECTED_AMOUNT)));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const addItem = nativeEditorButton(editor, 'AI_INPUT_BAR.MANUAL_ACTION');

        addItem.click();
        const draft = await waitForEmptyMealItemPickerAsync(harness);
        const draftAmount = mealQuantityInput(draft);
        expect(draftAmount.value).toBe('');
        enterMealQuantity(draftAmount, PRODUCT_AMOUNT);
        harness.detectChanges();
        expect(nativeEditorButton(draft, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').disabled).toBe(true);
        nativeEditorButton(draft, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            expect(draft.isConnected).toBe(false);
        });

        addItem.click();
        const reopenedDraft = await waitForEmptyMealItemPickerAsync(harness);
        expect(mealQuantityInput(reopenedDraft).value).toBe('');
        nativeEditorButton(reopenedDraft, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            expect(reopenedDraft.isConnected).toBe(false);
        });
        harness.detectChanges();
        const itemNames = Array.from(editor.querySelectorAll('.meal-manage__ai-summary-name'), item => item.textContent.trim());
        expect(itemNames).toEqual(['Green apple']);
        expect(api.update).not.toHaveBeenCalled();

        const original = openMealQuantityEditor(harness, editor);
        const amount = mealQuantityInput(original);
        const apply = nativeEditorButton(original, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE');
        expect(amount.valueAsNumber).toBe(PRODUCT_AMOUNT);
        amount.focus();
        amount.value = '';
        amount.dispatchEvent(new Event('input', { bubbles: true }));
        amount.blur();
        harness.detectChanges();
        expect(apply.disabled).toBe(true);
        expect(original.textContent).toContain('FORM_ERRORS.REQUIRED');

        enterMealQuantity(amount, CORRECTED_AMOUNT);
        harness.detectChanges();
        expect(apply.disabled).toBe(false);
        apply.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(original.isConnected).toBe(false);
        });
        nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await waitForDiaryAsync(harness);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({
                comment: ORIGINAL_NOTE,
                items: [{ productId: PRODUCT_ID, recipeId: null, amount: CORRECTED_AMOUNT, origin: 'Manual' }],
            }),
        );
    });
});

async function waitForEmptyMealItemPickerAsync(harness: RouterTestingHarness): Promise<HTMLElement> {
    const http = TestBed.inject(HttpTestingController);
    const dialog = await vi.waitFor(() => {
        harness.detectChanges();
        const panel = document.querySelector('fd-meal-manual-item-dialog');
        if (!(panel instanceof HTMLElement)) {
            throw new Error('Expected the manual item dialog');
        }
        expect(panel.querySelector('fd-item-select-dialog')).toBeTruthy();
        return panel;
    });
    http.expectOne(request => request.url.endsWith('/products/overview') && request.method === 'GET').flush({
        allProducts: { data: [], page: 1, limit: 10, totalPages: 0, totalItems: 0 },
        recentItems: [],
        favoriteItems: [],
        favoriteTotalCount: 0,
    });
    harness.detectChanges();
    expect(dialog.textContent).toContain('PRODUCT_LIST.NO_PRODUCTS_FOUND');
    expect(nativeEditorButton(dialog, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').disabled).toBe(true);
    return dialog;
}

describe('Routed meal manual nutrition warning', () => {
    const enteredCalories = 300;
    const calculatedCalories = 120;
    const calculatedCarbs = 30;
    const warningSelector = '.nutrition-editor-messages__warning';

    it.each([
        { action: 'keep manual', manualCalories: enteredCalories, automatic: false },
        { action: 'correct manually', manualCalories: calculatedCalories, automatic: false },
        { action: 'switch to automatic', manualCalories: undefined, automatic: true },
    ] as const)('shows the calorie mismatch and lets the user $action', async ({ action, manualCalories, automatic }) => {
        const api = setupEditorRoutes();
        const meal = savedManualMeal(enteredCalories, calculatedCarbs);
        api.getById.mockReturnValueOnce(of(meal));
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { NUTRITION_EDITOR: { CALORIES_MISMATCH_WARNING: 'Expected {{expected}}, entered {{actual}}' } });
        await firstValueFrom(translate.use('en'));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const calories = editor.querySelector<HTMLInputElement>('.nutrition-editor__input--calories input');
        if (calories === null) {
            throw new Error('Expected the meal calories control');
        }
        expect(calories.value).toBe(String(enteredCalories));
        expect(calories.readOnly).toBe(false);
        expect(editor.querySelector(warningSelector)?.textContent.trim()).toBe(
            `Expected ${calculatedCalories}, entered ${enteredCalories}`,
        );
        const save = nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON');
        expect(save.disabled).toBe(false);

        if (action === 'correct manually') {
            enterMealQuantity(calories, calculatedCalories);
        } else if (action === 'switch to automatic') {
            nativeEditorButton(editor, 'MEAL_MANAGE.NUTRITION_MODE.AUTO').click();
        }
        harness.detectChanges();
        expect(calories.value).toBe(String(manualCalories ?? calculatedCalories));
        expect(calories.readOnly).toBe(automatic);
        expect(editor.querySelector(warningSelector) !== null).toBe(action === 'keep manual');

        api.update.mockReturnValueOnce(of(meal));
        save.click();
        await waitForDiaryAsync(harness);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({
                isNutritionAutoCalculated: automatic,
                manualCalories,
                manualCarbs: automatic ? undefined : calculatedCarbs,
                items: [expect.objectContaining({ productId: PRODUCT_ID, amount: PRODUCT_AMOUNT })],
            }),
        );
    });
});

function savedManualMeal(calories: number, carbs: number): Meal {
    const productBaseAmount = 100;
    const productBaseCalories = 80;
    const productBaseCarbs = 20;
    const meal = savedMeal(ORIGINAL_NOTE, PRODUCT_AMOUNT, {
        ...createEmptyProductSnapshot(),
        id: PRODUCT_ID,
        name: 'Green apple',
        baseAmount: productBaseAmount,
        caloriesPerBase: productBaseCalories,
        carbsPerBase: productBaseCarbs,
    });
    return {
        ...meal,
        totalCalories: calories,
        totalProteins: 0,
        totalCarbs: carbs,
        isNutritionAutoCalculated: false,
        manualCalories: calories,
        manualProteins: 0,
        manualFats: 0,
        manualCarbs: carbs,
        manualAlcohol: 0,
    };
}

function openMealQuantityEditor(harness: RouterTestingHarness, editor: HTMLElement): HTMLElement {
    nativeEditorButton(editor, 'MEAL_MANAGE.MANUAL_ITEM_EDIT').click();
    harness.detectChanges();
    const dialog = document.querySelector('fd-meal-manual-item-dialog');
    if (!(dialog instanceof HTMLElement)) {
        throw new Error('Expected the meal quantity dialog');
    }
    return dialog;
}

function mealQuantityInput(dialog: HTMLElement): HTMLInputElement {
    const input = dialog.querySelector('input[type="number"]');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the editable meal quantity');
    }
    return input;
}

function enterMealQuantity(amount: HTMLInputElement, value: number): void {
    amount.focus();
    amount.value = String(value);
    amount.dispatchEvent(new Event('input', { bubbles: true }));
    amount.blur();
}

function requireEditor(harness: RouterTestingHarness, selector: string): HTMLElement {
    const editor = harness.routeNativeElement?.querySelector(selector);
    if (!(editor instanceof HTMLElement)) {
        throw new Error(`Expected a real lazy meal editor ${selector}`);
    }
    return editor;
}

function commentInput(editor: HTMLElement): HTMLTextAreaElement {
    const disclosure = editor.querySelector('details.meal-manage__optional');
    if (disclosure instanceof HTMLDetailsElement && !disclosure.open) {
        disclosure.querySelector('summary')?.click();
    }
    const comment = editor.querySelector('fd-meal-general-info textarea');
    if (!(comment instanceof HTMLTextAreaElement)) {
        throw new Error('Expected the meal comment field');
    }
    return comment;
}

function changeComment(comment: HTMLTextAreaElement, value: string): void {
    comment.value = value;
    comment.dispatchEvent(new Event('input', { bubbles: true }));
}

function satietyOption(host: ParentNode, label: string): HTMLButtonElement {
    const option = host.querySelector<HTMLButtonElement>(`fd-meal-satiety-fields button[role="radio"][aria-label^="${label}."]`);
    if (option === null) {
        throw new Error(`Expected the inline meal option "${label}"`);
    }
    return option;
}

function nativeEditorButton(host: ParentNode, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll('button')).find(
        candidate => candidate.getAttribute('aria-label') === label || candidate.textContent.trim().endsWith(label),
    );
    if (button === undefined) {
        throw new Error(`Expected the native meal editor action ${label}`);
    }
    return button;
}

async function chooseDepartureAsync(harness: RouterTestingHarness, label: string): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        const dialog = document.querySelector('fd-confirm-delete-dialog');
        if (dialog === null) {
            throw new Error('Expected the native discard confirmation');
        }
        nativeEditorButton(dialog, label).click();
    });
}

async function waitForDiaryAsync(harness: RouterTestingHarness): Promise<void> {
    await vi.waitFor(
        () => {
            harness.detectChanges();
            expect(TestBed.inject(Router).url).toBe(MEALS_URL);
            expect(harness.routeNativeElement?.querySelector('fd-meal-list')).not.toBeNull();
        },
        { timeout: LAZY_ROUTE_TIMEOUT_MS },
    );
}
