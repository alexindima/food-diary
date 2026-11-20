import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { FoodDiaryTranslationLoader } from '../../shared/i18n/food-diary-translation.loader';
import { createEmptyProductSnapshot, type Meal, type MealOverview, MealSourceType } from '../../shared/models/meal.data';
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

function setupEditorRoutes(consentGranted = true): MealEditorApi {
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
            { provide: AuthService, useValue: { isAuthenticated: signal(true), isPremium: signal(false), getUserId: (): null => null } },
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

function savedMeal(comment = ORIGINAL_NOTE, amount = PRODUCT_AMOUNT): Meal {
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
                product: { ...createEmptyProductSnapshot(), id: PRODUCT_ID, name: 'Green apple' },
                recipe: null,
            },
        ],
        aiSessions: [],
    };
}

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
        const api = setupEditorRoutes(false);
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
