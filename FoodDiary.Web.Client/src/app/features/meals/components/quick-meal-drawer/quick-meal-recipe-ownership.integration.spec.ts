import { type HttpRequest, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, RouterOutlet, type Routes, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import type { RecipeHttpResponse } from '../../../../shared/api/sdk/generated/model/recipe-http-response';
import { RecipeVisibility } from '../../../../shared/models/recipe.data';
import { QuickMealDrawerComponent } from './quick-meal-drawer';

const RECIPES_PATH = '/api/v1/recipes';
const MEALS_PATH = '/api/v1/meals';
const SAVED_AT = '2026-11-21T08:00:00Z';
const RECIPE = {
    id: 'catalog-soup',
    name: 'Catalog soup',
    servings: 1,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: SAVED_AT,
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
} satisfies RecipeHttpResponse;
const OTHER_RECIPE = { ...RECIPE, id: 'salad', name: 'Side salad' };
const GRAMS_PER_SERVING = 100;
const STALE_GRAMS_PER_SERVING = 80;
const MERGED_SERVINGS = 2;
const EDITED_SERVINGS = 3;

@Component({
    imports: [QuickMealDrawerComponent, RouterOutlet],
    template: `<fd-quick-meal-drawer />
        <main><router-outlet /></main>`,
})
class QuickMealRecipesRouteFixture {}

describe('Quick meal editing a recipe added again', () => {
    it('unlocks the replacement recipe and retains its new editor when the old lookup resolves', async () => {
        const { harness, http, host } = await renderRecipesAsync();
        const add = recipeCardAdd(host, RECIPE.name);
        expect(add.disabled).toBe(false);
        add.click();
        harness.detectChanges();
        expect(recipeAmount(host, RECIPE.name)).toContain('1 QUICK_MEAL.SERVINGS_ONE');

        recipeEdit(host, RECIPE.name).click();
        const previousLookup = http.expectOne(matches('GET', `${RECIPES_PATH}/${RECIPE.id}`));
        harness.detectChanges();
        expect(previousLookup.cancelled).toBe(false);
        expect(recipeEdit(host, RECIPE.name).disabled).toBe(true);
        expect(button(host, 'QUICK_MEAL.SAVE_ARIA').disabled).toBe(true);
        expect(document.querySelector('fd-meal-manual-item-dialog, .cdk-overlay-backdrop')).toBeNull();

        recipeCardAdd(host, OTHER_RECIPE.name).click();
        harness.detectChanges();
        expect(recipeAmount(host, OTHER_RECIPE.name)).toContain('1 QUICK_MEAL.SERVINGS_ONE');
        expect(recipeEdit(host, RECIPE.name).disabled).toBe(true);
        expect(button(host, 'QUICK_MEAL.SAVE_ARIA').disabled).toBe(true);

        const addAgain = recipeCardAdd(host, RECIPE.name);
        expect(addAgain.disabled).toBe(false);
        expect(addAgain.closest('[inert]')).toBeNull();
        addAgain.click();
        harness.detectChanges();
        expect(recipeAmount(host, RECIPE.name)).toContain('2 QUICK_MEAL.SERVINGS_OTHER');
        expect(document.querySelector('fd-meal-manual-item-dialog')).toBeNull();

        const edit = recipeEdit(host, RECIPE.name);
        const save = button(host, 'QUICK_MEAL.SAVE_ARIA');
        expect({ replacementEditDisabled: edit.disabled, draftSaveDisabled: save.disabled }).toEqual({
            replacementEditDisabled: false,
            draftSaveDisabled: false,
        });

        edit.click();
        http.expectOne(matches('GET', `${RECIPES_PATH}/${RECIPE.id}`)).flush(recipeRead(GRAMS_PER_SERVING));
        const editor = await waitForEditorAsync(harness);
        const amount = requiredElement(editor.querySelector<HTMLInputElement>('input[type="number"]'), 'recipe grams');
        expect(amount.valueAsNumber).toBe(MERGED_SERVINGS * GRAMS_PER_SERVING);
        amount.value = String(EDITED_SERVINGS * GRAMS_PER_SERVING);
        amount.dispatchEvent(new Event('input', { bubbles: true }));
        if (!previousLookup.cancelled) {
            previousLookup.flush(recipeRead(STALE_GRAMS_PER_SERVING));
        }
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(document.querySelectorAll('fd-meal-manual-item-dialog')).toHaveLength(1);
        expect(document.querySelector('fd-meal-manual-item-dialog')).toBe(editor);
        expect(amount.valueAsNumber).toBe(EDITED_SERVINGS * GRAMS_PER_SERVING);
        expect(save.disabled).toBe(true);

        const done = Array.from(editor.querySelectorAll('button')).find(
            candidate => candidate.querySelector('.fd-ui-button__label')?.textContent.trim() === 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE',
        );
        requiredElement(done ?? null, 'amount editor Save').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(recipeAmount(host, RECIPE.name)).toContain('3 QUICK_MEAL.SERVINGS_OTHER');
            expect(save.disabled).toBe(false);
        });
        save.click();
        const request = http.expectOne(matches('POST', MEALS_PATH));
        expect(request.request.body).toMatchObject({
            items: [
                { recipeId: RECIPE.id, productId: null, amount: EDITED_SERVINGS },
                { recipeId: OTHER_RECIPE.id, productId: null, amount: 1 },
            ],
        });
        completeMeal(request);
        harness.detectChanges();
        expect(host.querySelector('.quick-meal__item')).toBeNull();
    });
});

const MEASURED_GRAMS_PER_SERVING = 120;
const MIXED_RECIPE = {
    ...RECIPE,
    id: 'measured-soup',
    name: 'Measured soup',
    language: 'en',
    languageConfirmed: true,
    missingIngredientCount: 1,
    steps: [
        {
            id: 'mix-step',
            stepNumber: 1,
            instruction: 'Whisk the oats until smooth.',
            ingredients: [
                {
                    id: 'oats-ingredient',
                    amount: MEASURED_GRAMS_PER_SERVING,
                    productId: 'oats-product',
                    productName: 'Oats',
                    productBaseUnit: 'G',
                    productBaseAmount: 100,
                    productCaloriesPerBase: 380,
                    productProteinsPerBase: 13,
                    productFatsPerBase: 7,
                    productCarbsPerBase: 65,
                    productFiberPerBase: 10,
                    productAlcoholPerBase: 0,
                },
                { id: 'salt-ingredient', amount: 0, textName: 'Salt', amountText: 'a pinch' },
            ],
        },
    ],
} satisfies RecipeHttpResponse;
const gramsOnlyRecipe: RecipeHttpResponse = {
    ...MIXED_RECIPE,
    missingIngredientCount: 0,
    steps: MIXED_RECIPE.steps.map(step => ({ ...step, ingredients: step.ingredients.filter(item => item.productId !== undefined) })),
};

describe('Quick meal serving units after saving a recipe edit', () => {
    it('keeps the confirmed gram basis after an older mixed-ingredient read arrives', async () => {
        const { harness, http, host } = await renderRecipesAsync([MIXED_RECIPE]);
        const name = MIXED_RECIPE.name;
        recipeCardAdd(host, name).click();
        harness.detectChanges();
        recipeEdit(host, name).click();
        const obsoleteRead = http.expectOne(measuredRecipeRead(true));
        harness.detectChanges();
        expect(button(host, 'QUICK_MEAL.SAVE_ARIA').disabled).toBe(true);
        expect(document.querySelector('fd-meal-manual-item-dialog, .cdk-overlay-backdrop')).toBeNull();

        await removeTextIngredientAndSaveAsync(harness, http);
        expect(harness.routeNativeElement).toBe(host);
        const readd = recipeCardAdd(host, name);
        expect(readd.disabled).toBe(false);
        readd.click();
        harness.detectChanges();
        expect(recipeAmount(host, name)).toContain('2 QUICK_MEAL.SERVINGS_OTHER');

        recipeEdit(host, name).click();
        http.expectOne(measuredRecipeRead(true)).flush(gramsOnlyRecipe);
        const editor = await waitForEditorAsync(harness);
        const expectedGrams = MERGED_SERVINGS * MEASURED_GRAMS_PER_SERVING;
        expect(renderedRecipeQuantity(editor)).toEqual({ value: expectedGrams, label: GRAM_AMOUNT_LABEL });

        obsoleteRead.flush(MIXED_RECIPE);
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(document.querySelector('fd-meal-manual-item-dialog')).toBe(editor);
        expect(renderedRecipeQuantity(editor)).toEqual({ value: expectedGrams, label: GRAM_AMOUNT_LABEL });
        const save = button(host, 'QUICK_MEAL.SAVE_ARIA');
        editorButton(editor, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(document.querySelector('fd-meal-manual-item-dialog')).toBeNull();
            expect(save.disabled).toBe(false);
        });

        recipeEdit(host, name).click();
        http.expectNone(measuredRecipeRead(true));
        const reopened = await waitForEditorAsync(harness);
        expect(renderedRecipeQuantity(reopened)).toEqual({ value: expectedGrams, label: GRAM_AMOUNT_LABEL });

        const quantity = recipeQuantityInput(reopened);
        quantity.value = String(EDITED_SERVINGS * MEASURED_GRAMS_PER_SERVING);
        quantity.dispatchEvent(new Event('input', { bubbles: true }));
        harness.detectChanges();
        editorButton(reopened, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(recipeAmount(host, name)).toContain('3 QUICK_MEAL.SERVINGS_OTHER');
            expect(save.disabled).toBe(false);
        });
        save.click();
        const request = http.expectOne(matches('POST', MEALS_PATH));
        expect(request.request.body).toMatchObject({
            items: [{ recipeId: MIXED_RECIPE.id, productId: null, amount: EDITED_SERVINGS }],
        });
        completeMeal(request);
    });
});

const GRAM_AMOUNT_LABEL = 'MEAL_MANAGE.ADD_ITEM_ROW.AMOUNT (PRODUCT_AMOUNT_UNITS.G)';

async function removeTextIngredientAndSaveAsync(harness: RouterTestingHarness, http: HttpTestingController): Promise<void> {
    const navigation = harness.navigateByUrl(`/recipes/${MIXED_RECIPE.id}/edit`);
    const editorRead = await vi.waitFor(() => http.expectOne(measuredRecipeRead(false)));
    editorRead.flush(MIXED_RECIPE);
    await navigation;
    harness.detectChanges();
    const editorHost = requiredElement(harness.routeNativeElement, 'recipe editor route');

    const rows = Array.from(editorHost.querySelectorAll<HTMLElement>('.recipe-ingredient'));
    const salt = rows.find(row => row.querySelector<HTMLInputElement>('.recipe-ingredient__name input')?.value === 'Salt');
    button(requiredElement(salt ?? null, 'text ingredient row'), 'COMMON.DELETE').click();
    harness.detectChanges();
    expect(editorHost.querySelectorAll('.recipe-ingredient')).toHaveLength(1);
    requiredElement(editorHost.querySelector<HTMLButtonElement>('fd-recipe-manage button[type="submit"]'), 'recipe Save').click();

    const write = await vi.waitFor(() => http.expectOne(matches('PATCH', `${RECIPES_PATH}/${MIXED_RECIPE.id}`)));
    expect(write.request.body).toMatchObject({
        servings: 1,
        steps: [{ ingredients: [{ productId: 'oats-product', amount: MEASURED_GRAMS_PER_SERVING }] }],
    });
    write.flush(gramsOnlyRecipe);
    const returned = await vi.waitFor(() => http.expectOne(matches('GET', `${RECIPES_PATH}/overview`)));
    flushCatalog(returned, [gramsOnlyRecipe]);
    harness.detectChanges();
}

function measuredRecipeRead(includePublic: boolean): (request: HttpRequest<unknown>) => boolean {
    const byId = matches('GET', `${RECIPES_PATH}/${MIXED_RECIPE.id}`);
    return request => byId(request) && request.params.get('includePublic') === String(includePublic);
}

function recipeQuantityInput(editor: HTMLElement): HTMLInputElement {
    return requiredElement(editor.querySelector<HTMLInputElement>('input[type="number"]'), 'recipe amount');
}

function renderedRecipeQuantity(editor: HTMLElement): { value: number; label: string | null } {
    const input = recipeQuantityInput(editor);
    return { value: input.valueAsNumber, label: input.getAttribute('aria-label') };
}

function editorButton(editor: HTMLElement, label: string): HTMLButtonElement {
    const match = Array.from(editor.querySelectorAll('button')).find(
        candidate => candidate.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    return requiredElement(match ?? null, label);
}

async function waitForEditorAsync(harness: RouterTestingHarness): Promise<HTMLElement> {
    return vi.waitFor(() => {
        harness.detectChanges();
        return requiredElement(document.querySelector<HTMLElement>('fd-meal-manual-item-dialog'), 'recipe amount editor');
    });
}

function recipeRead(gramsPerServing: number): RecipeHttpResponse {
    return { ...RECIPE, steps: [{ ingredients: [{ amount: gramsPerServing, productBaseUnit: 'G' }] }] };
}

function completeMeal(request: TestRequest): void {
    request.flush({
        id: 'saved-catalog-soup',
        date: SAVED_AT,
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

async function renderRecipesAsync(
    recipes: readonly RecipeHttpResponse[] = [RECIPE, OTHER_RECIPE],
): Promise<{ harness: RouterTestingHarness; http: HttpTestingController; host: HTMLElement }> {
    vi.stubGlobal('matchMedia', (media: string) => ({
        matches: false,
        media,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(() => true),
    }));
    stubScrollIntoView();
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            provideRouter(
                [
                    {
                        path: 'recipes',
                        component: QuickMealRecipesRouteFixture,
                        loadChildren: async (): Promise<{ default: Routes }> => import('../../../recipes/recipe.routes'),
                    },
                ],
                withComponentInputBinding(),
            ),
            {
                provide: AuthService,
                useValue: { isPremium: signal(false), isAuthenticated: signal(true), getUserId: (): string => 'quick-meal-recipe-user' },
            },
            { provide: FdUiToastService, useValue: { success: vi.fn(), error: vi.fn() } },
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
    const harness = await RouterTestingHarness.create('/recipes');
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/core.json?')).flush({});
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/app.json?')).flush({});
    flushCatalog(http.expectOne(matches('GET', `${RECIPES_PATH}/overview`)), recipes);
    harness.detectChanges();
    return { harness, http, host: requiredElement(harness.routeNativeElement, 'recipe route') };
}

function flushCatalog(request: TestRequest, recipes: readonly RecipeHttpResponse[]): void {
    request.flush({
        recentItems: [],
        favoriteItems: [],
        favoriteTotalCount: 0,
        allRecipes: { page: 1, limit: 10, totalItems: recipes.length, totalPages: 1, data: recipes },
    });
}

function recipeCardAdd(host: HTMLElement, name: string): HTMLButtonElement {
    const recipe = Array.from(host.querySelectorAll<HTMLElement>('fd-recipe-card')).find(
        card => card.querySelector(`button[aria-label="${name}"]`) !== null,
    );
    return button(requiredElement(recipe ?? null, `catalog card for ${name}`), 'RECIPE_CARD.ADD_TO_MEAL');
}

function recipeRow(host: HTMLElement, name: string): HTMLElement {
    const row = Array.from(host.querySelectorAll<HTMLElement>('.quick-meal__item')).find(
        item => item.querySelector('.quick-meal__name')?.textContent.trim() === name,
    );
    return requiredElement(row ?? null, `draft row for ${name}`);
}

function recipeEdit(host: HTMLElement, name: string): HTMLButtonElement {
    return button(recipeRow(host, name), 'QUICK_MEAL.EDIT_ITEM_NAMED');
}

function recipeAmount(host: HTMLElement, name: string): string | null {
    return requiredElement(recipeRow(host, name).querySelector('.quick-meal__meta'), 'recipe quantity').textContent;
}

function button(host: HTMLElement, label: string): HTMLButtonElement {
    return requiredElement(host.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`), label);
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
