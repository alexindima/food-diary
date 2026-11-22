import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicIngredientsComponent } from './public-ingredients';

describe('public ingredient quantities', () => {
    beforeEach(() =>
        TestBed.configureTestingModule({
            imports: [PublicIngredientsComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        }),
    );
    it('scales numbers, preserves free text and does not link hidden ingredients', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            { name: 'rice', amount: 100, unit: 'g', amountText: null, recipeId: null, isAvailable: true },
            { name: 'соль', amount: null, unit: null, amountText: 'to taste', recipeId: null, isAvailable: true },
            { name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
        ];
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings);
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected component element');
        }
        const decrease = element.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.FEWER"]');
        const increase = element.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]');
        increase?.click();
        fixture.detectChanges();
        expect(element.textContent).toContain('150');
        expect(element.textContent).toContain('Rice');
        expect(element.textContent).toContain('Соль');
        expect(element.textContent).toContain('to taste');
        expect(element.textContent).toContain('PUBLIC_RECIPES.PRIVATE_INGREDIENT');
        expect(element.querySelectorAll('li a')).toHaveLength(0);
        decrease?.click();
        decrease?.click();
        fixture.detectChanges();
        expect(decrease?.disabled).toBe(true);
    });

    it('sends scaled quantities and free text, with no shopping action for private ingredients', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            {
                name: 'rice',
                productId: entityId<'product'>('public-rice'),
                amount: 100,
                unit: 'Gram',
                amountText: null,
                recipeId: null,
                isAvailable: true,
            },
            { name: 'Salt', amount: null, unit: null, amountText: 'to taste', recipeId: null, isAvailable: true },
            { name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
        ];
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings * 2);
        const added = vi.fn();
        fixture.componentInstance.shoppingAdd.subscribe(added);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const buttons = element.querySelectorAll<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.SHOPPING_ADD"]');
        expect(buttons).toHaveLength(2);
        const visiblePreviews = element.querySelectorAll(
            'fd-ui-button:not([style*="display: none"]) button[aria-label="PUBLIC_RECIPES.PRODUCT_VIEW"]',
        );
        expect(visiblePreviews).toHaveLength(1);
        buttons[0].click();
        buttons[1].click();
        expect(added).toHaveBeenNthCalledWith(1, {
            index: 0,
            item: { name: 'rice', productId: 'public-rice', amount: 200, unit: 'G', note: null, isChecked: false },
        });
        expect(added).toHaveBeenNthCalledWith(2, {
            index: 1,
            item: { name: 'Salt', productId: null, amount: null, unit: null, note: 'to taste', isChecked: false },
        });
        expect(element.querySelector('.ingredient-list')?.textContent).toContain('Rice');
        expect(recipe.steps[0].ingredients[0].name).toBe('rice');
    });
});

describe('public ingredient names', () => {
    beforeEach(() =>
        TestBed.configureTestingModule({
            imports: [PublicIngredientsComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        }),
    );

    it('capitalizes linked recipe ingredients while preserving their links and internal case', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            { name: 'sauce (GF)', amount: 1, unit: 'serving', amountText: null, recipeId: entityId<'recipe'>('sauce'), isAvailable: true },
        ];
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings);
        fixture.detectChanges();
        const link = (fixture.nativeElement as HTMLElement).querySelector('li a');
        expect(link?.textContent).toBe('Sauce (GF)');
        expect(link?.getAttribute('href')).toBe('/explore/sauce');
        expect(recipe.steps[0].ingredients[0].name).toBe('sauce (GF)');
    });
});

describe('public ingredient empty state', () => {
    beforeEach(() =>
        TestBed.configureTestingModule({
            imports: [PublicIngredientsComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        }),
    );

    it('hides the portions hint when the recipe has no ingredients', () => {
        const recipe = publicRecipeFixture();
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('[aria-label="PUBLIC_RECIPES.PORTIONS_HINT"]')).toBeNull();
        expect(element.textContent).toContain('PUBLIC_RECIPES.NO_INGREDIENTS');
    });
});

const DEFAULT_LIMIT = 50;
const LARGE_RECIPE_SERVINGS = 80;

it('shows shopping errors once and only repeats them for a new failed attempt', () => {
    TestBed.configureTestingModule({
        imports: [PublicIngredientsComponent],
        providers: [provideRouter([]), provideTranslateTesting()],
    });
    const toast = TestBed.inject(FdUiToastService);
    const originalError = toast.error.bind(toast);
    const boundedRef = toast.info('Existing notification', { duration: 0 });
    const showError = vi.spyOn(toast, 'error');
    // Allow the real state update once so a broken effect cannot loop indefinitely.
    showError.mockImplementation(message => (showError.mock.calls.length === 1 ? originalError(message, { duration: 0 }) : boundedRef));
    const recipe = publicRecipeFixture();
    const fixture = TestBed.createComponent(PublicIngredientsComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.componentRef.setInput('servings', recipe.servings);
    fixture.componentRef.setInput('shoppingMessage', 'PUBLIC_RECIPES.SHOPPING_ERROR');
    fixture.detectChanges();

    expect(showError).toHaveBeenCalledOnce();
    toast.info('Unrelated notification', { duration: 0 });
    fixture.detectChanges();
    expect(showError).toHaveBeenCalledOnce();

    fixture.componentRef.setInput('shoppingMessage', null);
    fixture.detectChanges();
    fixture.componentRef.setInput('shoppingMessage', 'PUBLIC_RECIPES.SHOPPING_ERROR');
    fixture.detectChanges();
    expect(showError).toHaveBeenCalledTimes(2);
});

it.each([2, LARGE_RECIPE_SERVINGS])('limits servings while preserving larger original recipes (%s)', original => {
    TestBed.configureTestingModule({
        imports: [PublicIngredientsComponent],
        providers: [provideRouter([]), provideTranslateTesting()],
    });
    const recipe = publicRecipeFixture();
    recipe.servings = original;
    const limit = Math.max(DEFAULT_LIMIT, original);
    const fixture = TestBed.createComponent(PublicIngredientsComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.componentRef.setInput('servings', limit - 1);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const increase = element.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]');
    increase?.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.servings()).toBe(limit);
    expect(increase?.disabled).toBe(true);
    increase?.click();
    expect(fixture.componentInstance.servings()).toBe(limit);
});

it('hides shopping actions when all ingredients are unavailable', () => {
    TestBed.configureTestingModule({ imports: [PublicIngredientsComponent], providers: [provideRouter([]), provideTranslateTesting()] });
    const recipe = publicRecipeFixture();
    recipe.steps[0].ingredients = [{ name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false }];
    const fixture = TestBed.createComponent(PublicIngredientsComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.componentRef.setInput('servings', recipe.servings);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector<HTMLElement>('.shopping-toolbar')?.style.display).toBe('none');
});

it('opens the public product from an ingredient and dismisses an unavailable preview without leaving the recipe', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    TestBed.configureTestingModule({
        imports: [PublicIngredientsComponent],
        providers: [provideRouter([]), provideTranslateTesting(), provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        http.verify();
    });
    const recipe = publicRecipeFixture();
    const productId = entityId<'product'>('oats');
    recipe.steps[0].ingredients = [{ name: 'oats', productId, amount: 50, unit: 'g', amountText: null, recipeId: null, isAvailable: true }];
    const fixture = TestBed.createComponent(PublicIngredientsComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.componentRef.setInput('servings', recipe.servings);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector<HTMLButtonElement>('button[aria-label="PUBLIC_RECIPES.PRODUCT_VIEW"]');
    if (link === null) {
        throw new Error('Expected an available product preview action');
    }
    const originalUrl = TestBed.inject(Router).url;
    const productUrl = `${environment.apiUrls.products}/public/${productId}`;
    const preview = (): HTMLElement | null => document.querySelector('fd-public-product-preview');
    const openPreviewAsync = async (): Promise<TestRequest> => {
        link.click();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(preview()?.querySelector('fd-ui-loader')).toBeInstanceOf(HTMLElement);
        });
        return http.expectOne({ url: productUrl, method: 'GET' });
    };
    const closePreviewAsync = async (): Promise<void> => {
        const close = document.querySelector<HTMLButtonElement>('fd-public-product-preview button[aria-label="COMMON.CLOSE"]');
        if (close === null) {
            throw new Error('Expected a close action in the product preview');
        }
        close.click();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(preview()).toBeNull();
        });
    };

    const published = await openPreviewAsync();
    published.flush({
        id: productId,
        name: 'Rolled oats',
        baseAmount: 100,
        baseUnit: 'Gram',
        calories: 375,
        proteins: 13,
        fats: 7,
        carbs: 63,
        fiber: 10,
        alcohol: 0,
    });
    TestBed.tick();
    expect(preview()?.querySelector('.product-details h2')?.textContent).toBe('Rolled oats');
    expect(preview()?.querySelector('.calories dd')?.textContent).toContain('375');
    await closePreviewAsync();

    const unpublished = await openPreviewAsync();
    unpublished.flush(null, {
        status: HttpStatusCode.NotFound,
        statusText: 'Not Found',
    });
    TestBed.tick();
    expect(preview()?.querySelector('[role="status"]')?.textContent).toContain('PUBLIC_RECIPES.PRODUCT_UNAVAILABLE');
    await closePreviewAsync();
    expect(TestBed.inject(Router).url).toBe(originalUrl);
    expect(link.isConnected).toBe(true);
});

describe('Public ingredient actions for mixed sources', () => {
    it('adds only remaining visible ingredients and preserves unsupported or absent units', () => {
        TestBed.configureTestingModule({
            imports: [PublicIngredientsComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        });
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            { name: 'rice', amount: 100, unit: 'g', amountText: null, recipeId: null, isAvailable: true },
            { name: 'Sauce', amount: 0.5, unit: 'serving', amountText: null, recipeId: entityId<'recipe'>('sauce'), isAvailable: true },
            { name: 'Spice', amount: 1, unit: 'pinch', amountText: null, recipeId: null, isAvailable: true },
            { name: 'Water', amount: 2, unit: null, amountText: null, recipeId: null, isAvailable: true },
            { name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
        ];
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        const addAll = vi.fn();
        fixture.componentInstance.shoppingAddAll.subscribe(addAll);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings * 2);
        fixture.componentRef.setInput('addedIngredients', new Set([0]));
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const add = Array.from(root.querySelectorAll<HTMLButtonElement>('.shopping-toolbar button')).find(
            control => control.querySelector('.fd-ui-button__label')?.textContent.trim() === 'PUBLIC_RECIPES.SHOPPING_ADD_ALL',
        );

        add?.click();

        expect(addAll).toHaveBeenCalledExactlyOnceWith([
            {
                index: 1,
                item: { name: 'Sauce', productId: null, amount: 1, unit: null, note: 'PUBLIC_RECIPES.SERVINGS', isChecked: false },
            },
            { index: 2, item: { name: 'Spice', productId: null, amount: 2, unit: null, note: 'pinch', isChecked: false } },
            { index: 3, item: { name: 'Water', productId: null, amount: 4, unit: null, note: null, isChecked: false } },
        ]);
    });
});
