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
import {
    createEmptyProductSnapshot,
    type Meal,
    type MealAiSession,
    type MealOverview,
    MealSourceType,
    type ProductMealItem,
} from '../../shared/models/meal.data';
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

describe('Routed meal type after changing the local time', () => {
    const breakfastTime = '08:15';

    it.each([
        { url: ADD_URL, initial: 'LUNCH', after: 'BREAKFAST' },
        { url: `${ADD_URL}?mealType=DINNER`, initial: 'DINNER', after: 'DINNER' },
    ])('uses the expected type for $url when the clock selection changes', async ({ url, initial, after }) => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date('2026-05-04T13:00:00'));
        onTestFinished(() => {
            vi.useRealTimers();
        });
        setupEditorRoutes();
        const harness = await RouterTestingHarness.create(url);
        const editor = requireEditor(harness, 'fd-meal-add');
        const typeControl = editor.querySelector('fd-meal-general-info fd-ui-select');
        expect(typeControl?.textContent).toContain(`MEAL_TYPES.${initial}`);

        selectMealEditorTime(editor, breakfastTime);
        harness.detectChanges();
        expect(typeControl?.textContent).toContain(`MEAL_TYPES.${after}`);
    });

    it('retains the saved meal type and dirty note across a query change and a new time', async () => {
        const api = setupEditorRoutes();
        api.update.mockReturnValueOnce(of(savedMeal()));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const changedNote = 'Dinner remains dinner at another time';
        changeComment(commentInput(editor), changedNote);
        harness.detectChanges();

        await harness.navigateByUrl(`${MEAL_URL}?mealType=BREAKFAST`);
        expect(requireEditor(harness, 'fd-meal-edit')).toBe(editor);
        expect(commentInput(editor).value).toBe(changedNote);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(MEAL_ID);

        selectMealEditorTime(editor, breakfastTime);
        harness.detectChanges();
        expect(editor.querySelector('fd-meal-general-info fd-ui-select')?.textContent).toContain('MEAL_TYPES.DINNER');
        nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await waitForDiaryAsync(harness);
        const expectedDate = new Date(savedMeal().date);
        const [hour, minute] = breakfastTime.split(':').map(Number);
        expectedDate.setHours(hour, minute, 0, 0);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({ mealType: 'DINNER', comment: changedNote, date: expectedDate }),
        );
    });
});

const mealClockGap = new Map([
    ['America/Los_Angeles', { savedUtc: '2026-03-08T09:30:00.000Z', correctedUtc: '2026-03-08T10:30:00.000Z' }],
    ['Asia/Tbilisi', { savedUtc: '2005-03-26T22:30:00.000Z', correctedUtc: '2005-03-26T23:30:00.000Z' }],
]).get(new Intl.DateTimeFormat().resolvedOptions().timeZone);
if (mealClockGap !== undefined) {
    it('shows the nonexistent native local time error and saves the corrected instant', async () => {
        const api = setupEditorRoutes();
        const message = 'This time does not exist on this date in your time zone. Choose another time.';
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { MEAL_MANAGE: { NONEXISTENT_TIME: message } });
        await firstValueFrom(translate.use('en'));
        const original = { ...savedMeal(), date: utcInstant(mealClockGap.savedUtc) };
        api.getById.mockReturnValueOnce(of(original));
        api.update.mockReturnValueOnce(of({ ...original, date: utcInstant(mealClockGap.correctedUtc) }));

        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        const time = editor.querySelector<HTMLInputElement>('fd-ui-time-input input[role="combobox"]');
        expect(time?.value).toBe('01:30');
        const save = nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON');

        selectMealEditorTime(editor, '02:30');
        harness.detectChanges();
        expect(save.disabled).toBe(false);
        save.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(editor.querySelector('fd-ui-time-input [role="alert"]')?.textContent.trim()).toBe(message);
        });
        expect(time?.getAttribute('aria-invalid')).toBe('true');
        expect(time?.value).toBe('02:30');
        expect(api.update).not.toHaveBeenCalled();
        expect(TestBed.inject(Router).url).toBe(MEAL_URL);

        selectMealEditorTime(editor, '03:30');
        harness.detectChanges();
        expect(editor.querySelector('fd-ui-time-input [role="alert"]')).toBeNull();
        save.click();
        await waitForDiaryAsync(harness);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({ date: new Date(mealClockGap.correctedUtc), mealType: 'DINNER', comment: ORIGINAL_NOTE }),
        );
    });
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

describe('Returning to the meal diary after saving a meal', () => {
    it('keeps the active diary retry and shows the committed meal without saving again', async () => {
        const api = setupEditorRoutes();
        const saving = new Subject<Meal>();
        const retryingOverview = new Subject<MealOverview>();
        const note = 'Updated dinner in the diary';
        const saved = savedMeal(note);
        api.update.mockReturnValueOnce(saving);
        api.queryOverview.mockReturnValueOnce(throwError(() => new Error('Diary unavailable'))).mockReturnValueOnce(retryingOverview);
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-edit');
        changeComment(commentInput(editor), note);
        harness.detectChanges();
        nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await vi.waitFor(() => {
            expect(api.update).toHaveBeenCalledExactlyOnceWith(MEAL_ID, expect.objectContaining({ comment: note }));
        });
        expect(TestBed.inject(Router).url).toBe(MEAL_URL);
        expect(api.queryOverview).not.toHaveBeenCalled();

        saving.next(saved);
        saving.complete();
        await waitForDiaryAsync(harness);
        const diary = requireEditor(harness, 'fd-meal-list');
        expect(diary.querySelector('[role="alert"]')?.textContent).toContain('ERRORS.LOAD_FAILED_TITLE');
        const retry = nativeEditorButton(diary, 'ERRORS.RETRY');
        retry.click();
        retry.click();

        expect(api.queryOverview).toHaveBeenCalledTimes(2);
        expect(retryingOverview.observed).toBe(true);
        harness.detectChanges();
        expect(diary.querySelector('fd-skeleton-card')).not.toBeNull();
        expect(diary.querySelector('fd-error-state')).toBeNull();

        retryingOverview.next({ ...DIARY, allMeals: { ...DIARY.allMeals, data: [saved], totalItems: 1 } });
        retryingOverview.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(diary.querySelector('fd-meal-card')?.textContent).toContain(note);
        });
        expect(api.update).toHaveBeenCalledTimes(1);
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
    it('starts another meal with its recognition items collapsed even when the editor is reused', async () => {
        const api = setupEditorRoutes({ premium: true });
        const nextMealId = entityId<'meal'>('next-dinner');
        api.getById
            .mockReturnValueOnce(of(mealWithRecognition(MEAL_ID, ORIGINAL_NOTE, 'Broccoli', 'Carrots', 'Spinach')))
            .mockReturnValueOnce(of(mealWithRecognition(nextMealId, 'Next dinner', 'Cauliflower', 'Peas', 'Cabbage')));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const originalPreview = requireEditor(harness, 'fd-meal-ai-sessions');

        expect(originalPreview.textContent).toContain('Broccoli');
        expect(originalPreview.textContent).not.toContain('Spinach');
        expect(recognitionPreviewToggle(originalPreview).getAttribute('aria-expanded')).toBe('false');
        recognitionPreviewToggle(originalPreview).click();
        harness.detectChanges();
        expect(originalPreview.textContent).toContain('Spinach');
        expect(recognitionPreviewToggle(originalPreview).getAttribute('aria-expanded')).toBe('true');

        await harness.navigateByUrl(`/meals/${nextMealId}/edit`);
        const nextPreview = requireEditor(harness, 'fd-meal-ai-sessions');
        expect(nextPreview).toBe(originalPreview);
        expect(nextPreview.textContent).toContain('Cauliflower');
        expect(nextPreview.textContent).toContain('Peas');
        expect(nextPreview.textContent).not.toContain('Spinach');
        expect(nextPreview.textContent).not.toContain('Cabbage');
        expect(recognitionPreviewToggle(nextPreview).getAttribute('aria-expanded')).toBe('false');

        recognitionPreviewToggle(nextPreview).click();
        harness.detectChanges();
        expect(nextPreview.textContent).toContain('Cabbage');
        expect(recognitionPreviewToggle(nextPreview).getAttribute('aria-expanded')).toBe('true');
        expect(api.update).not.toHaveBeenCalled();
    });

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

describe('Recognition row removal on the meal edit route', () => {
    it('does not remove the next recognition when the same visible delete button receives a second click', async () => {
        const api = setupEditorRoutes({ premium: true });
        const firstSession = recognitionSession(MEAL_ID, 'broccoli-recognition', ['Broccoli']);
        const nextSession = recognitionSession(MEAL_ID, 'cauliflower-recognition', ['Cauliflower']);
        const remaining = { ...savedMeal(), aiSessions: [nextSession] };
        api.getById.mockReturnValueOnce(of({ ...remaining, aiSessions: [firstSession, nextSession] })).mockReturnValueOnce(of(remaining));
        api.update.mockReturnValueOnce(of(remaining)).mockReturnValueOnce(of({ ...remaining, aiSessions: [] }));
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const preview = requireEditor(harness, 'fd-meal-ai-sessions');
        const firstRow = recognitionRow(preview, 'Broccoli');
        const remove = nativeEditorButton(firstRow, 'MEAL_MANAGE.ITEMS_AI_DELETE');

        remove.click();
        expect(remove.isConnected).toBe(true);
        expect(firstRow.textContent).toContain('Broccoli');
        remove.click();
        harness.detectChanges();

        const editor = requireEditor(harness, 'fd-meal-manage-form');
        const remainingPreviewText = editor.querySelector('fd-meal-ai-sessions')?.textContent ?? '';
        expect(remainingPreviewText).toContain('Cauliflower');
        expect(remainingPreviewText).not.toContain('Broccoli');
        nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        const router = TestBed.inject(Router);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url.split('?')[0]).toBe(MEALS_URL);
        });
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({
                aiSessions: [expect.objectContaining({ items: [expect.objectContaining({ nameEn: 'Cauliflower' })] })],
            }),
        );

        await harness.navigateByUrl(MEAL_URL);
        const freshPreview = requireEditor(harness, 'fd-meal-ai-sessions');
        nativeEditorButton(recognitionRow(freshPreview, 'Cauliflower'), 'MEAL_MANAGE.ITEMS_AI_DELETE').click();
        harness.detectChanges();
        const freshEditor = requireEditor(harness, 'fd-meal-manage-form');
        expect(freshEditor.querySelector('fd-meal-ai-sessions')).toBeNull();
        nativeEditorButton(freshEditor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.update).toHaveBeenNthCalledWith(2, MEAL_ID, expect.objectContaining({ aiSessions: [] }));
        });
    });
});

it('does not open a neighboring recognition from a removed row edit button', async () => {
    const api = setupEditorRoutes({ premium: true });
    const broccoli = recognitionSession(MEAL_ID, 'broccoli-recognition', ['Broccoli']);
    const cauliflower = recognitionSession(MEAL_ID, 'cauliflower-recognition', ['Cauliflower']);
    api.getById.mockReturnValueOnce(of({ ...savedMeal(), aiSessions: [broccoli, cauliflower] }));
    const harness = await RouterTestingHarness.create(MEAL_URL);
    const preview = requireEditor(harness, 'fd-meal-ai-sessions');
    const firstRow = recognitionRow(preview, 'Broccoli');
    const edit = nativeEditorButton(firstRow, 'MEAL_MANAGE.ITEMS_AI_EDIT');
    expect(edit.disabled).toBe(false);

    nativeEditorButton(firstRow, 'MEAL_MANAGE.ITEMS_AI_DELETE').click();
    expect(edit.isConnected).toBe(true);
    expect(firstRow.textContent).toContain('Broccoli');
    edit.click();
    await vi.dynamicImportSettled();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(document.querySelector('fd-meal-photo-recognition-dialog')?.textContent ?? null).toBeNull();
    const nextPreview = requireEditor(harness, 'fd-meal-ai-sessions');
    expect(nextPreview.textContent).not.toContain('Broccoli');
    nativeEditorButton(recognitionRow(nextPreview, 'Cauliflower'), 'MEAL_MANAGE.ITEMS_AI_EDIT').click();
    const review = await waitForRecognitionReviewAsync(harness, 'Cauliflower');
    nativeEditorButton(review, 'COMMON.CANCEL').click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector('fd-meal-photo-recognition-dialog')).toBeNull();
    });
    expect(api.update).not.toHaveBeenCalled();
});

describe('Manual row removal on the meal edit route', () => {
    it('keeps the later manual items in order after a second click on the same visible remove button', async () => {
        const api = setupEditorRoutes();
        const original = savedMeal();
        const bananaAmount = 60;
        const carrotAmount = 25;
        const banana = savedProductItem('banana', 'Banana', bananaAmount);
        const carrot = savedProductItem('carrot', 'Carrot', carrotAmount);
        const remaining = { ...original, items: [banana, carrot] };
        api.getById.mockReturnValueOnce(of({ ...original, items: [...original.items, banana, carrot] })).mockReturnValueOnce(of(remaining));
        api.update.mockReturnValueOnce(of(remaining)).mockReturnValueOnce(of({ ...remaining, items: [carrot] }));
        const router = TestBed.inject(Router);
        const harness = await RouterTestingHarness.create(MEAL_URL);
        const editor = requireEditor(harness, 'fd-meal-manage-form');
        const firstRow = manualItemRow(editor, 'Green apple');
        const remove = nativeEditorButton(firstRow, 'MEAL_MANAGE.REMOVE_ITEM');

        remove.click();
        expect(remove.isConnected).toBe(true);
        expect(firstRow.textContent).toContain('Green apple');
        remove.click();
        harness.detectChanges();

        expect(manualItemNames(editor)).toEqual(['Banana', 'Carrot']);
        nativeEditorButton(editor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url.split('?')[0]).toBe(MEALS_URL);
        });
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            MEAL_ID,
            expect.objectContaining({
                items: [
                    { productId: banana.product.id, recipeId: null, amount: banana.amount, origin: 'Manual' },
                    { productId: carrot.product.id, recipeId: null, amount: carrot.amount, origin: 'Manual' },
                ],
            }),
        );

        await harness.navigateByUrl(MEAL_URL);
        const freshEditor = requireEditor(harness, 'fd-meal-manage-form');
        nativeEditorButton(manualItemRow(freshEditor, 'Banana'), 'MEAL_MANAGE.REMOVE_ITEM').click();
        harness.detectChanges();
        expect(manualItemNames(freshEditor)).toEqual(['Carrot']);
        nativeEditorButton(freshEditor, 'MEAL_MANAGE.SAVE_BUTTON').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.update).toHaveBeenNthCalledWith(
                2,
                MEAL_ID,
                expect.objectContaining({
                    items: [{ productId: carrot.product.id, recipeId: null, amount: carrot.amount, origin: 'Manual' }],
                }),
            );
        });
    });
});

function savedProductItem(productId: string, name: string, amount: number): ProductMealItem & { product: Product } {
    return {
        id: entityId<'meal-item'>(`${productId}-portion`),
        mealId: MEAL_ID,
        amount: productQuantityFromStored(amount),
        sourceType: MealSourceType.Product,
        product: { ...createEmptyProductSnapshot(), id: entityId<'product'>(productId), name },
        recipe: null,
    };
}

function manualItemRow(editor: HTMLElement, name: string): HTMLElement {
    const rows = [...editor.querySelectorAll<HTMLElement>('.meal-manage__manual-summary')];
    const row = rows.find(candidate => candidate.querySelector('.meal-manage__ai-summary-name')?.textContent.trim() === name);
    if (row === undefined) {
        throw new Error(`Expected the visible manual row for ${name}`);
    }
    return row;
}

function manualItemNames(editor: HTMLElement): string[] {
    return [...editor.querySelectorAll('.meal-manage__manual-summary .meal-manage__ai-summary-name')].map(node => node.textContent.trim());
}

function recognitionRow(preview: HTMLElement, food: string): HTMLElement {
    const rows = [...preview.querySelectorAll<HTMLElement>('.meal-manage__ai-summary')];
    const row = rows.find(item => item.textContent.includes(food));
    if (row === undefined) {
        throw new Error(`Expected the visible recognition row for ${food}`);
    }
    return row;
}

function recognitionPreviewToggle(preview: HTMLElement): HTMLButtonElement {
    const button = preview.querySelector('.meal-manage__ai-summary-more button');
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error('Expected a recognition preview expansion button');
    }
    return button;
}

function mealWithRecognition(mealId: Meal['id'], comment: string, food: string, ...additionalItems: string[]): Meal {
    const meal = savedMeal(comment);
    const sessionId = `${mealId}-recognition`;
    return {
        ...meal,
        id: mealId,
        items: meal.items.map(item => ({ ...item, id: entityId<'meal-item'>(`${mealId}-portion`), mealId })),
        aiSessions: [recognitionSession(mealId, sessionId, [food, ...additionalItems])],
    };
}

function recognitionSession(mealId: Meal['id'], sessionId: string, foods: readonly string[]): MealAiSession {
    return {
        id: sessionId,
        mealId,
        imageUrl: `/images/${mealId}.jpg`,
        recognizedAtUtc: '2026-05-04T15:00:00Z',
        items: foods.map((nameEn, index) => ({
            id: `${sessionId}-item-${index}`,
            sessionId,
            nameEn,
            amount: 100,
            unit: 'g',
            calories: 34,
            proteins: 3,
            fats: 0,
            carbs: 7,
            fiber: 3,
            alcohol: 0,
        })),
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

function selectMealEditorTime(editor: ParentNode, time: string): void {
    const control = editor.querySelector<HTMLInputElement>('fd-ui-time-input input[role="combobox"]');
    if (control === null) {
        throw new Error('Expected the native meal time control');
    }
    control.click();
    TestBed.tick();
    const hours = document.querySelector('[role="group"][aria-label="MEAL_DETAILS.HOURS"]');
    const minutes = document.querySelector('[role="group"][aria-label="MEAL_DETAILS.MINUTES"]');
    if (hours === null || minutes === null) {
        throw new Error('Expected the rendered hours and minutes');
    }
    const [hour, minute] = time.split(':');
    nativeEditorButton(hours, hour).click();
    TestBed.tick();
    nativeEditorButton(minutes, minute).click();
    TestBed.tick();
    expect(control.value).toBe(time);
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
