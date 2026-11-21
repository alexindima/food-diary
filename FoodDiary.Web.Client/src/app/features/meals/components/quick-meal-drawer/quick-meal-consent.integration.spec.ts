import { type HttpRequest, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { SessionEventsService } from '../../../../shared/auth/session-events.service';
import { createEmptyProductSnapshot, type Meal, MealSourceType } from '../../../../shared/models/meal.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { MAXIMUM_CONSUMED_QUANTITY, productQuantityFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { PrivacyConsentPurpose } from '../../../../shared/privacy/privacy-consent-purpose';
import { PRIVACY_NOTICE_VERSION } from '../../../../shared/privacy/privacy-notice-version';
import { ProductDetailComponent } from '../../../products/components/detail/product-detail/product-detail';
import { QuickMealService } from '../../lib/quick/quick-meal.service';
import { MealManageFormComponent } from '../manage/meal-manage-form';
import { QuickMealDrawerComponent } from './quick-meal-drawer';

const MEALS_PATH = '/api/v1/meals';
const PRIVACY_PATH = '/api/v1/users/privacy';
const PRODUCT = { ...createEmptyProductSnapshot(), id: entityId<'product'>('catalog-pear'), name: 'Pear', defaultPortionAmount: 120 };
const CONSENT_STATE = {
    noticeVersion: PRIVACY_NOTICE_VERSION,
    healthAcceptedAtUtc: null,
    healthDeclinedAtUtc: '2026-11-01T10:00:00Z',
};
const GRANTED_AT = '2026-11-21T10:00:00Z';

describe('Quick meal recipe serving limit', () => {
    it('saves the maximum allowed serving count from the gram editor', async () => {
        const gramsPerServing = 200.14;
        // The Meals create contract allows 1,000,000 servings. At 200.14 g each the exact boundary is 200,140,000 g.
        const maximumGrams = 200_140_000;
        const recipe: Recipe = {
            id: entityId<'recipe'>('known-gram-recipe'),
            name: 'One serving soup',
            servings: 1,
            visibility: RecipeVisibility.Private,
            usageCount: 0,
            createdAt: utcInstant(GRANTED_AT),
            isOwnedByCurrentUser: true,
            isNutritionAutoCalculated: true,
            steps: [],
        };
        const { fixture, http, host, showError } = await renderDrawerAsync(draft => {
            draft.addRecipe(recipe);
        });

        requiredElement(
            host.querySelector<HTMLButtonElement>('button[aria-label="QUICK_MEAL.EDIT_ITEM_NAMED"]'),
            'the recipe edit action',
        ).click();
        http.expectOne(matches('GET', `/api/v1/recipes/${recipe.id}`)).flush({
            id: recipe.id,
            servings: 1,
            steps: [{ ingredients: [{ amount: gramsPerServing, productBaseUnit: 'G' }] }],
        });
        const editor = await vi.waitFor(() => {
            fixture.detectChanges();
            return requiredElement(document.querySelector<HTMLElement>('fd-meal-manual-item-dialog'), 'the gram editor');
        });
        const amount = requiredElement(editor.querySelector<HTMLInputElement>('input[type="number"]'), 'the grams input');
        const done = dialogButton(editor, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE');
        expect(amount.valueAsNumber).toBe(gramsPerServing);
        amount.value = String(maximumGrams + 1);
        amount.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(done.disabled).toBe(true);
        done.click();
        expect(editor.isConnected).toBe(true);
        http.expectNone(matches('POST', MEALS_PATH));

        amount.value = String(maximumGrams);
        amount.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(done.disabled).toBe(false);
        done.click();
        const save = requiredElement(host.querySelector<HTMLButtonElement>('button[aria-label="QUICK_MEAL.SAVE_ARIA"]'), 'Quick Meal Save');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(document.querySelector('fd-meal-manual-item-dialog')).toBeNull();
            expect(host.querySelector('.quick-meal__meta')?.textContent).toContain(MAXIMUM_CONSUMED_QUANTITY.toLocaleString('en'));
            expect(save.disabled).toBe(false);
        });
        save.click();

        const requests = http.match(matches('POST', MEALS_PATH));
        expect({
            mealWrites: requests.map(request => {
                const body: unknown = request.request.body;
                return body;
            }),
            errors: showError.mock.calls,
        }).toEqual({
            mealWrites: [
                expect.objectContaining({
                    items: [{ recipeId: recipe.id, productId: null, amount: MAXIMUM_CONSUMED_QUANTITY }],
                }),
            ],
            errors: [],
        });
        completeMeal(requests[0]);
        fixture.detectChanges();
        expect(host.querySelector('.quick-meal__item')).toBeNull();
    });
});

describe('Quick meal combined quantity limit', () => {
    it('keeps an overflowing combined quantity editable instead of submitting it', async () => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        const save = requiredElement(host.querySelector<HTMLButtonElement>('button[aria-label="QUICK_MEAL.SAVE_ARIA"]'), 'Quick Meal Save');
        await changeQuickMealAmountAsync(fixture, host, MAXIMUM_CONSUMED_QUANTITY);

        TestBed.inject(FdUiDialogService).open(ProductDetailComponent, { data: PRODUCT });
        http.expectOne(matches('GET', `/api/v1/favorite-products/check/${PRODUCT.id}`)).flush(false);
        TestBed.tick();
        const details = requiredElement(document.querySelector<HTMLElement>('fd-product-detail'), 'the product details');
        expect(details.querySelector('.product-detail__title')?.textContent).toContain(PRODUCT.name);
        dialogButton(details, 'PRODUCT_CARD.ADD_TO_MEAL').click();
        fixture.detectChanges();

        const overflow = MAXIMUM_CONSUMED_QUANTITY + PRODUCT.defaultPortionAmount;
        expect(host.querySelectorAll('.quick-meal__item')).toHaveLength(1);
        expect(host.querySelector('.quick-meal__meta')?.textContent).toContain(overflow.toLocaleString('en'));

        save.click();
        const attemptedWrites = http.match(matches('POST', MEALS_PATH)).map((request): unknown => {
            const body: unknown = request.request.body;
            request.flush({ error: 'Validation.Invalid' }, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
            return body;
        });
        expect(attemptedWrites).toEqual([]);
        expect(showError).toHaveBeenCalledExactlyOnceWith('MEAL_MANAGE.MAX_AMOUNT_ERROR');
        expect(host.querySelector('.quick-meal__name')?.textContent.trim()).toBe(PRODUCT.name);
        expect(save.disabled).toBe(false);

        await changeQuickMealAmountAsync(fixture, host, MAXIMUM_CONSUMED_QUANTITY);
        save.click();
        const submitted = http.expectOne(matches('POST', MEALS_PATH));
        expect(submitted.request.body).toMatchObject({
            items: [{ productId: PRODUCT.id, recipeId: null, amount: MAXIMUM_CONSUMED_QUANTITY }],
        });
        completeMeal(submitted);
        fixture.detectChanges();
        expect(host.querySelector('.quick-meal__item')).toBeNull();
    });
});

async function changeQuickMealAmountAsync(
    fixture: ComponentFixture<QuickMealDrawerComponent>,
    host: HTMLElement,
    amount: number,
): Promise<void> {
    requiredElement(
        host.querySelector<HTMLButtonElement>('.quick-meal__item-action button[aria-label="QUICK_MEAL.EDIT_ITEM_NAMED"]'),
        'the edit action',
    ).click();
    const editor = await vi.waitFor(() => {
        fixture.detectChanges();
        return requiredElement(document.querySelector<HTMLElement>('fd-meal-manual-item-dialog'), 'the amount editor');
    });
    const amountInput = requiredElement(editor.querySelector<HTMLInputElement>('input[type="number"]'), 'the amount input');
    amountInput.value = String(amount);
    amountInput.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    const done = dialogButton(editor, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE');
    expect(done.disabled).toBe(false);
    done.click();
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('.quick-meal__meta')?.textContent).toContain(amount.toLocaleString('en'));
    });
}

function dialogButton(dialog: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(dialog.querySelectorAll<HTMLButtonElement>('button')).find(
        candidate => candidate.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    return requiredElement(button ?? null, label);
}

describe('Quick meal consent from the catalog drawer', () => {
    it('offers the health review on a consent rejection and saves the same draft after acceptance', async () => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        const submitted = requestConsentReview(host, http);

        const dialog = await openConsentDialogAsync(http, fixture);
        expect(host.querySelector('.quick-meal__name')?.textContent.trim()).toBe(PRODUCT.name);
        expect(host.querySelector('fieldset')?.getAttribute('aria-busy')).toBe('true');
        expect(showError).not.toHaveBeenCalled();
        acceptConsent(dialog, fixture);

        await confirmChoiceAsync(http, true);
        const retried = await expectRequestAsync(http, 'POST', MEALS_PATH);

        const key = submitted.request.headers.get('Idempotency-Key');
        expect(key).toBeTruthy();
        expect(retried.request.headers.get('Idempotency-Key')).toBe(key);
        expect(retried.request.body).toEqual(submitted.request.body);
        expect(retried.request.body).toMatchObject({
            items: [{ productId: PRODUCT.id, recipeId: null, amount: PRODUCT.defaultPortionAmount }],
        });
        completeMeal(retried);
        fixture.detectChanges();

        expect(host.querySelector('.quick-meal__name')).toBeNull();
    });
});

describe('Quick meal after health review is declined or unavailable', () => {
    it('keeps the draft available after recording an explicit decline', async () => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        requestConsentReview(host, http);
        const dialog = await openConsentDialogAsync(http, fixture);
        clickConsentAction(dialog, 'PRIVACY_CHOICES.DECLINE');
        await confirmChoiceAsync(http, false);

        await expectDraftUnlockedAsync(fixture, host);
        http.expectNone(matches('POST', MEALS_PATH));
        expect(showError).not.toHaveBeenCalled();
    });

    it.each([
        { reload: false, error: 'PRIVACY_CHOICES.ERROR' },
        { reload: true, error: 'PRIVACY_CHOICES.RELOAD' },
    ])('retains the meal and explains privacy recovery failure $error', async ({ reload, error }) => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        requestConsentReview(host, http);
        const health = await expectRequestAsync(http, 'GET', PRIVACY_PATH);
        if (reload) {
            health.flush({ ...CONSENT_STATE, noticeVersion: 'older-notice' });
        } else {
            health.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        }

        await expectDraftUnlockedAsync(fixture, host);
        expect(showError).toHaveBeenCalledExactlyOnceWith(error);
        expect(document.querySelector('fd-health-consent-dialog')).toBeNull();
        http.expectNone(matches('POST', MEALS_PATH));
    });
});

describe('Quick meal consent retry limit', () => {
    it('keeps the draft and shows a save error if the authorized retry is rejected again', async () => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        requestConsentReview(host, http);
        const dialog = await openConsentDialogAsync(http, fixture);
        acceptConsent(dialog, fixture);
        await confirmChoiceAsync(http, true);

        const retried = await expectRequestAsync(http, 'POST', MEALS_PATH);
        rejectConsent(retried);
        await expectDraftUnlockedAsync(fixture, host);

        expect(showError).toHaveBeenCalledExactlyOnceWith('QUICK_MEAL.SAVE_ERROR');
        http.expectNone(matches('GET', PRIVACY_PATH));
        http.expectNone(matches('POST', MEALS_PATH));
    });
});

describe('Quick meal after a delayed health choice', () => {
    it('does not retry an old save when the account changes before the consent confirmation arrives', async () => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        requestConsentReview(host, http);
        const dialog = await openConsentDialogAsync(http, fixture);
        acceptConsent(dialog, fixture);
        const pendingConfirmation = await requestChoiceConfirmationAsync(http, true);

        const sessions = TestBed.inject(SessionEventsService);
        sessions.notifySessionEnded();
        sessions.notifyAuthenticated();
        const newProduct = { ...PRODUCT, id: entityId<'product'>('new-session-apple'), name: 'Apple' };
        TestBed.inject(QuickMealService).addProduct(newProduct);
        pendingConfirmation.flush({ ...CONSENT_STATE, healthAcceptedAtUtc: GRANTED_AT });
        await fixture.whenStable();
        fixture.detectChanges();

        http.expectNone(matches('POST', MEALS_PATH));
        expect(host.querySelector('.quick-meal__name')?.textContent.trim()).toBe(newProduct.name);
        expect(host.querySelector('fieldset')?.getAttribute('aria-busy')).toBe('false');
        expect(showError).not.toHaveBeenCalled();
    });
});

describe('Saved meal editor after an explicit health consent decline', () => {
    it('keeps both drafts when the editor receives an earlier consent rejection after the user declines', async () => {
        const { fixture, http, host, showError } = await renderDrawerAsync();
        const editor = TestBed.createComponent(MealManageFormComponent);
        const editorHost = editor.nativeElement as HTMLElement;
        const meal = existingMeal();
        const updatePath = `${MEALS_PATH}/${meal.id}`;
        editor.componentRef.setInput('meal', meal);
        editor.detectChanges();
        const note = requiredElement(editorHost.querySelector('textarea'), 'the saved meal comment');
        const form = requiredElement(editorHost.querySelector('form'), 'the saved meal form');
        note.value = 'Keep this unsaved edit';
        note.dispatchEvent(new Event('input', { bubbles: true }));
        editor.detectChanges();
        form.requestSubmit();

        const accepted = await expectRequestAsync(http, 'GET', PRIVACY_PATH);
        accepted.flush({ ...CONSENT_STATE, healthAcceptedAtUtc: '2026-10-31T10:00:00Z', healthDeclinedAtUtc: null });
        const update = await expectRequestAsync(http, 'PATCH', updatePath);
        expect(update.request.body).toMatchObject({ comment: note.value });
        editor.detectChanges();
        expect(form.getAttribute('aria-busy')).toBe('true');

        requestConsentReview(host, http);
        const dialog = await openConsentDialogAsync(http, fixture);
        clickConsentAction(dialog, 'PRIVACY_CHOICES.DECLINE');
        await confirmChoiceAsync(http, false);
        await expectDraftUnlockedAsync(fixture, host);
        expect(form.getAttribute('aria-busy')).toBe('true');

        rejectConsent(update);
        await vi.waitFor(() => {
            editor.detectChanges();
            expect(form.getAttribute('aria-busy')).toBe('false');
        });
        expect(note.value).toBe('Keep this unsaved edit');
        expect(editorHost.querySelector('.meal-manage__footer-error')).toBeNull();
        expect(host.querySelector('.quick-meal__name')?.textContent.trim()).toBe(PRODUCT.name);
        expect(showError).not.toHaveBeenCalled();
        http.expectNone(matches('PATCH', updatePath));
        http.expectNone(matches('POST', MEALS_PATH));
    });
});

function existingMeal(): Meal {
    const id = entityId<'meal'>('existing-lunch');
    return {
        id,
        date: utcInstant('2026-11-20T12:00:00Z'),
        mealType: 'LUNCH',
        isNutritionAutoCalculated: true,
        totalCalories: 0,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        items: [
            {
                id: entityId<'meal-item'>('existing-lunch-pear'),
                mealId: id,
                sourceType: MealSourceType.Product,
                product: PRODUCT,
                recipe: null,
                amount: productQuantityFromStored(PRODUCT.defaultPortionAmount),
            },
        ],
    };
}

async function renderDrawerAsync(
    seedDraft: (draft: QuickMealService) => void = draft => {
        draft.addProduct(PRODUCT);
    },
): Promise<{
    fixture: ComponentFixture<QuickMealDrawerComponent>;
    http: HttpTestingController;
    host: HTMLElement;
    showError: ReturnType<typeof vi.fn<FdUiToastService['error']>>;
}> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const showError = vi.fn<FdUiToastService['error']>();
    TestBed.configureTestingModule({
        imports: [QuickMealDrawerComponent, MealManageFormComponent],
        providers: [
            provideRouter([]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: { isPremium: signal(false), isAuthenticated: signal(true), getUserId: (): string => 'meal-consent-user' },
            },
            { provide: FdUiToastService, useValue: { success: vi.fn(), error: showError } },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        http.verify();
        vi.unstubAllGlobals();
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('en'));
    seedDraft(TestBed.inject(QuickMealService));
    const fixture = TestBed.createComponent(QuickMealDrawerComponent);
    fixture.detectChanges();
    return { fixture, http, host: fixture.nativeElement as HTMLElement, showError };
}

function requestConsentReview(host: HTMLElement, http: HttpTestingController): TestRequest {
    requiredElement(
        host.querySelector<HTMLButtonElement>('button[aria-label="QUICK_MEAL.SAVE_ARIA"]'),
        'the quick meal save action',
    ).click();
    const request = http.expectOne(matches('POST', MEALS_PATH));
    rejectConsent(request);
    return request;
}

function rejectConsent(request: TestRequest): void {
    request.flush(
        { error: 'Privacy.HealthConsentRequired', message: 'Review your health data processing choices in privacy settings.' },
        { status: HttpStatusCode.Forbidden, statusText: 'Forbidden' },
    );
}

function acceptConsent(dialog: HTMLElement, fixture: ComponentFixture<QuickMealDrawerComponent>): void {
    requiredElement(dialog.querySelector<HTMLInputElement>('input[type="checkbox"]'), 'the health agreement').click();
    fixture.detectChanges();
    clickConsentAction(dialog, 'PRIVACY_CHOICES.ACCEPT_AND_SAVE');
}

async function openConsentDialogAsync(
    http: HttpTestingController,
    fixture: ComponentFixture<QuickMealDrawerComponent>,
): Promise<HTMLElement> {
    const state = await expectRequestAsync(http, 'GET', PRIVACY_PATH);
    state.flush(CONSENT_STATE);
    return vi.waitFor(() => {
        fixture.detectChanges();
        return requiredElement(document.querySelector<HTMLElement>('fd-health-consent-dialog'), 'the health consent dialog');
    });
}

function clickConsentAction(dialog: HTMLElement, label: string): void {
    const button = Array.from(dialog.querySelectorAll<HTMLButtonElement>('button')).find(
        candidate => candidate.textContent.trim() === label,
    );
    requiredElement(button ?? null, label).click();
}

async function confirmChoiceAsync(http: HttpTestingController, granted: boolean): Promise<void> {
    const confirmed = await requestChoiceConfirmationAsync(http, granted);
    confirmed.flush({
        ...CONSENT_STATE,
        healthAcceptedAtUtc: granted ? GRANTED_AT : null,
    });
}

async function requestChoiceConfirmationAsync(http: HttpTestingController, granted: boolean): Promise<TestRequest> {
    const state = await expectRequestAsync(http, 'GET', PRIVACY_PATH);
    state.flush(CONSENT_STATE);
    const save = await expectRequestAsync(http, 'PUT', PRIVACY_PATH);
    expect(save.request.body).toEqual({ purpose: PrivacyConsentPurpose.HealthTracking, granted, noticeVersion: PRIVACY_NOTICE_VERSION });
    save.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    return expectRequestAsync(http, 'GET', PRIVACY_PATH);
}

async function expectDraftUnlockedAsync(fixture: ComponentFixture<QuickMealDrawerComponent>, host: HTMLElement): Promise<void> {
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('fieldset')?.getAttribute('aria-busy')).toBe('false');
    });
    expect(host.querySelector('.quick-meal__name')?.textContent.trim()).toBe(PRODUCT.name);
}

async function expectRequestAsync(http: HttpTestingController, method: string, path: string): Promise<TestRequest> {
    return vi.waitFor(() => http.expectOne(matches(method, path), `${method} ${path}`));
}

function matches(method: string, path: string): (request: HttpRequest<unknown>) => boolean {
    return request => request.method === method && request.url.endsWith(path);
}

function requiredElement<T extends Element>(element: T | null, label: string): T {
    if (element === null) {
        throw new Error(`Expected ${label}`);
    }
    return element;
}

function completeMeal(request: TestRequest): void {
    request.flush({
        id: 'saved-quick-meal',
        date: GRANTED_AT,
        totalCalories: 0,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
    });
}
