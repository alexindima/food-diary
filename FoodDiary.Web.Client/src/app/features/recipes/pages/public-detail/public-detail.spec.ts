import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import type { RecipeId } from '../../../../shared/models/semantics/entity-id';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { PublicAuthDialogService } from '../../../public/contracts/auth-dialog';
import { ShoppingListAddFacade } from '../../../shopping-lists/contracts/shopping-list-add';
import { PublicRecipeService } from '../../api/public-recipe.service';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import type { PublicRecipe } from '../../models/public-recipe.data';
import { type PublicRecipePageData, publicRecipeResolver } from '../../resolvers/public-recipe.resolver';
import { PublicRecipeDetailComponent } from './public-detail';

const SELECTED_DIARY_SERVINGS = 3;
const PREP_ONLY_MINUTES = 12;
const COOK_ONLY_MINUTES = 25;

const authenticated = signal(false);
const facade = { isFavorite: vi.fn(() => of(false)), saveAsync: vi.fn(), addToDiaryAsync: vi.fn() };
const dialog = { openAsync: vi.fn() };
const browser = { getOrigin: vi.fn<() => string | undefined>(), copyTextAsync: vi.fn() };
const getById = vi.fn<PublicRecipeService['getById']>();
beforeEach(() => {
    authenticated.set(false);
    vi.clearAllMocks();
    browser.getOrigin.mockReturnValue('https://fooddiary.test');
    browser.copyTextAsync.mockResolvedValue(undefined);
    dialog.openAsync.mockResolvedValue(null);
    facade.saveAsync.mockResolvedValue(undefined);
    getById.mockImplementation(id =>
        of(
            id === 'other-recipe'
                ? { ...publicRecipeFixture(), id, name: 'Stew', prepTime: null, cookTime: null, language: 'ru' }
                : {
                      ...publicRecipeFixture(),
                      id,
                  },
        ),
    );
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [
                    {
                        path: 'explore',
                        component: PublicRecipeDetailComponent,
                        data: { seo: { recipe: publicRecipeFixture(), error: null } satisfies PublicRecipePageData },
                    },
                    {
                        path: 'explore/:id',
                        component: PublicRecipeDetailComponent,
                        resolve: { seo: publicRecipeResolver },
                        runGuardsAndResolvers: 'always',
                    },
                ],
                withComponentInputBinding(),
            ),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: PublicAuthDialogService, useValue: dialog },
            { provide: BrowserWindowService, useValue: browser },
            { provide: PublicRecipeService, useValue: { getById } },
        ],
    });
    TestBed.overrideComponent(PublicRecipeDetailComponent, {
        set: {
            providers: [
                { provide: PublicRecipesFacade, useValue: facade },
                {
                    provide: ShoppingListAddFacade,
                    useValue: {
                        setScope: vi.fn(),
                        isAdded: (): boolean => false,
                        busy: signal(false),
                        target: signal(null),
                        message: signal(null),
                    },
                },
            ],
        },
    });
});

describe('Public recipe actions when the route selects another recipe', () => {
    it('does not save the second recipe when login for the first recipe finishes late', async () => {
        const closed = new Subject<void>();
        dialog.openAsync.mockResolvedValueOnce({ afterClosed: () => closed.asObservable() });
        const harness = await RouterTestingHarness.create('/explore/recipe');
        recipeAction(harness, 'PUBLIC_RECIPES.SAVE').click();
        await harness.fixture.whenStable();
        expect(dialog.openAsync).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ returnUrl: '/explore/recipe' }));

        await harness.navigateByUrl('/explore/other-recipe');
        authenticated.set(true);
        closed.next();
        closed.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Stew');
        expect(facade.saveAsync).not.toHaveBeenCalled();
        expect(recipeAction(harness, 'PUBLIC_RECIPES.SAVE').disabled).toBe(false);
    });

    it.each([true, false])('keeps the new recipe actionable when an older save completes (success=%s)', async priorSaveSucceeded => {
        authenticated.set(true);
        const original = new Subject<void>();
        const replacement = new Subject<void>();
        facade.saveAsync.mockReturnValueOnce(firstValueFrom(original)).mockReturnValueOnce(firstValueFrom(replacement));
        const harness = await RouterTestingHarness.create('/explore/recipe');
        recipeAction(harness, 'PUBLIC_RECIPES.SAVE').click();
        harness.detectChanges();
        expect(recipeAction(harness, 'PUBLIC_RECIPES.SAVE').disabled).toBe(true);

        await harness.navigateByUrl('/explore/other-recipe');
        const saveSecond = recipeAction(harness, 'PUBLIC_RECIPES.SAVE');
        expect(saveSecond.disabled).toBe(false);
        saveSecond.click();
        harness.detectChanges();
        expect(facade.saveAsync).toHaveBeenNthCalledWith(2, expect.objectContaining({ id: 'other-recipe' }));

        if (priorSaveSucceeded) {
            original.next();
            original.complete();
        } else {
            original.error(new Error('The previous request failed'));
        }
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(recipeAction(harness, 'PUBLIC_RECIPES.SAVE').disabled).toBe(true);
        expect(harness.routeNativeElement?.querySelector('.recipe-summary [role="status"]:not([hidden])')).toBeNull();

        replacement.next();
        replacement.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(recipeAction(harness, 'PUBLIC_RECIPES.SAVED').disabled).toBe(true);
    });

    it.each([true, false])('ignores a late clipboard result from the previous recipe (success=%s)', async copied => {
        const pending = new Subject<void>();
        browser.copyTextAsync.mockReturnValueOnce(firstValueFrom(pending));
        const harness = await RouterTestingHarness.create('/explore/recipe');
        recipeAction(harness, 'PUBLIC_RECIPES.SHARE').click();
        expect(browser.copyTextAsync).toHaveBeenCalledExactlyOnceWith('https://fooddiary.test/explore/recipe');

        await harness.navigateByUrl('/explore/other-recipe');
        if (copied) {
            pending.next();
            pending.complete();
        } else {
            pending.error(new Error('Clipboard access denied'));
        }
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(harness.routeNativeElement?.querySelector('a[href="https://fooddiary.test/explore/recipe"]')).toBeNull();
        expect(harness.routeNativeElement?.querySelector('.recipe-summary [role="status"]:not([hidden])')).toBeNull();
        recipeAction(harness, 'PUBLIC_RECIPES.SHARE').click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(browser.copyTextAsync).toHaveBeenLastCalledWith('https://fooddiary.test/explore/other-recipe');
        expect(harness.routeNativeElement?.querySelector('.recipe-summary [role="status"]')?.textContent).toContain(
            'PUBLIC_RECIPES.COPIED',
        );
    });
});

describe('Public recipe data from the active route', () => {
    it.each([
        { prepTime: PREP_ONLY_MINUTES, cookTime: null, minutes: PREP_ONLY_MINUTES, label: 'PUBLIC_RECIPES.PREP_DETAIL' },
        { prepTime: null, cookTime: COOK_ONLY_MINUTES, minutes: COOK_ONLY_MINUTES, label: 'PUBLIC_RECIPES.COOK_DETAIL' },
    ])('shows the available time total and only $label', async ({ prepTime, cookTime, minutes, label }) => {
        getById.mockReturnValueOnce(of({ ...publicRecipeFixture(), prepTime, cookTime }));
        const harness = await RouterTestingHarness.create('/explore/recipe');
        const summary = harness.routeNativeElement?.querySelector('.recipe-summary');
        expect(summary?.querySelector('.recipe-facts .recipe-fact:first-child:not([hidden]) strong')?.textContent).toContain(
            `${minutes.toString()} PUBLIC_RECIPES.MIN`,
        );
        const visibleParts = Array.from(summary?.querySelectorAll('.recipe-time-breakdown:not([hidden]) span:not([hidden])') ?? []);
        expect(visibleParts.map(part => part.textContent.trim())).toEqual([label]);
    });

    it('keeps Save available after the initial favorite lookup fails and replaces the error after saving', async () => {
        authenticated.set(true);
        facade.isFavorite.mockReturnValueOnce(throwError(() => new Error('Favorite status unavailable')));
        const pending = new Subject<void>();
        facade.saveAsync.mockReturnValueOnce(firstValueFrom(pending));
        const harness = await RouterTestingHarness.create('/explore/recipe');
        const status = harness.routeNativeElement?.querySelector<HTMLElement>('.recipe-summary [role="status"]');
        expect(facade.isFavorite).toHaveBeenCalledExactlyOnceWith('recipe');
        expect(status?.hidden).toBe(false);
        expect(status?.textContent).toContain('PUBLIC_RECIPES.ACTION_ERROR');
        const save = recipeAction(harness, 'PUBLIC_RECIPES.SAVE');
        expect(save.disabled).toBe(false);

        save.click();
        harness.detectChanges();
        expect(save.disabled).toBe(true);
        expect(recipeAction(harness, 'PUBLIC_RECIPES.ADD_DIARY').disabled).toBe(true);
        expect(facade.saveAsync).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'recipe' }));
        expect(status?.hidden).toBe(true);

        pending.next();
        pending.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(status?.hidden).toBe(false);
        expect(status?.textContent).toContain('PUBLIC_RECIPES.SAVED');
        expect(recipeAction(harness, 'PUBLIC_RECIPES.SAVED').disabled).toBe(true);
        expect(recipeAction(harness, 'PUBLIC_RECIPES.ADD_DIARY').disabled).toBe(false);
    });
});

describe('Public recipe loading feedback', () => {
    it('shows the active save failure and allows a successful retry', async () => {
        authenticated.set(true);
        facade.saveAsync.mockRejectedValueOnce(new Error('Unavailable'));
        const harness = await RouterTestingHarness.create('/explore/recipe');

        recipeAction(harness, 'PUBLIC_RECIPES.SAVE').click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        const status = harness.routeNativeElement?.querySelector('.recipe-summary [role="status"]');
        expect(status?.textContent).toContain('PUBLIC_RECIPES.ACTION_ERROR');
        const save = recipeAction(harness, 'PUBLIC_RECIPES.SAVE');
        expect(save.disabled).toBe(false);

        save.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(status?.textContent).toContain('PUBLIC_RECIPES.SAVED');
        expect(facade.saveAsync).toHaveBeenCalledTimes(2);
    });

    it('retries an unavailable response through the same public route and renders the recovered recipe', async () => {
        getById.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable })));
        const harness = await RouterTestingHarness.create('/explore/other-recipe');
        const detail = harness.routeNativeElement;
        if (detail === null) {
            throw new Error('Expected the public recipe route to render.');
        }
        expect(detail.querySelector('h1')?.textContent).toBe('PUBLIC_RECIPES.ERROR');
        expect(detail.querySelector('fd-public-recipe-gallery')).toBeNull();
        recipeAction(harness, 'PUBLIC_RECIPES.RETRY').click();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(getById.mock.calls).toEqual([['other-recipe'], ['other-recipe']]);
        expect(harness.routeNativeElement).toBe(detail);
        expect(detail.querySelector('h1')?.textContent).toBe('Stew');
        const cover = detail.querySelector<HTMLImageElement>('fd-public-recipe-gallery .gallery-cover img');
        expect(cover?.getAttribute('src')).toBe('cover.jpg');
        expect(cover?.alt).toBe('Stew');
        expect(detail.querySelectorAll('fd-public-recipe-gallery .gallery-thumbs button')).toHaveLength(2);
        expect(detail.querySelector('.recipe-facts')?.textContent).toContain('RECIPE_LANGUAGE.RU');
        expect(detail.querySelector<HTMLElement>('.recipe-time-breakdown')?.hidden).toBe(true);
    });
});

it('ignores a competing diary action in the same turn as Save and allows it after Save finishes', async () => {
    authenticated.set(true);
    const saved = new Subject<void>();
    facade.saveAsync.mockReturnValueOnce(firstValueFrom(saved));
    const harness = await RouterTestingHarness.create('/explore/recipe');
    const save = recipeAction(harness, 'PUBLIC_RECIPES.SAVE');
    const add = recipeAction(harness, 'PUBLIC_RECIPES.ADD_DIARY');

    save.click();
    add.click();
    expect(facade.saveAsync).toHaveBeenCalledOnce();
    expect(facade.addToDiaryAsync).not.toHaveBeenCalled();
    harness.detectChanges();
    expect(add.disabled).toBe(true);

    saved.next();
    saved.complete();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(add.disabled).toBe(false);
    add.click();
    await harness.fixture.whenStable();
    expect(facade.addToDiaryAsync).toHaveBeenCalledExactlyOnceWith('recipe', publicRecipeFixture().servings);
});

function recipeAction(harness: RouterTestingHarness, label: string): HTMLButtonElement {
    const actions = harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('button');
    const selected = Array.from(actions ?? []).find(button => button.querySelector('.fd-ui-button__label')?.textContent.trim() === label);
    if (selected === undefined) {
        throw new Error(`Expected the recipe action ${label}`);
    }
    return selected;
}
describe('Public recipe QR code', () => {
    it('opens a QR code for the current public recipe without clipboard or sign-in access', async () => {
        const open = vi.fn().mockReturnValue({ afterClosed: vi.fn().mockReturnValue(of(undefined)), close: vi.fn() });
        TestBed.overrideProvider(FdUiDialogService, { useValue: { open } });
        const harness = await RouterTestingHarness.create('/explore/recipe?page=2&category=salads#ingredients');

        recipeAction(harness, 'PUBLIC_RECIPES.SHOW_QR').click();
        await vi.dynamicImportSettled();
        expect(open).toHaveBeenCalledExactlyOnceWith(expect.any(Function), {
            size: 'sm',
            data: { url: 'https://fooddiary.test/explore/recipe' },
        });

        await harness.navigateByUrl('/explore/other-recipe');
        recipeAction(harness, 'PUBLIC_RECIPES.SHOW_QR').click();
        await vi.dynamicImportSettled();
        expect(open).toHaveBeenNthCalledWith(2, expect.any(Function), {
            size: 'sm',
            data: { url: 'https://fooddiary.test/explore/other-recipe' },
        });
        expect(browser.copyTextAsync).not.toHaveBeenCalled();
        expect(dialog.openAsync).not.toHaveBeenCalled();
    });
});

describe('public recipe account actions', () => {
    it('copies the recipe URL without catalog filters', async () => {
        const harness = await RouterTestingHarness.create('/explore/recipe?page=2&category=salads');
        harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('.recipe-actions button')[2].click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(browser.copyTextAsync).toHaveBeenCalledWith('https://fooddiary.test/explore/recipe');
        expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain('PUBLIC_RECIPES.COPIED');
    });

    it('shows a selectable link when clipboard access fails', async () => {
        browser.copyTextAsync.mockRejectedValue(new Error('Clipboard unavailable'));
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('.recipe-actions button')[2].click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('a[href="https://fooddiary.test/explore/recipe"]')?.textContent).toContain(
            'https://fooddiary.test/explore/recipe',
        );
    });
    it('does not copy a URL when the browser origin is unavailable', async () => {
        browser.getOrigin.mockReturnValue(undefined);
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('.recipe-actions button')[2].click();
        await harness.fixture.whenStable();
        expect(browser.copyTextAsync).not.toHaveBeenCalled();
    });
    it('keeps catalog filters and pagination when returning', async () => {
        const harness = await RouterTestingHarness.create('/explore/recipe?page=2&category=salads&language=all&sortBy=fastest');
        harness.routeNativeElement?.querySelector<HTMLButtonElement>('button[aria-label="PUBLIC_RECIPES.CATALOG"]')?.click();
        await waitForAsyncTasksAsync();
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe('/explore?page=2&category=salads&language=all&sortBy=fastest');
    });
    it('prompts guests and retains the recipe URL without writing favorites', async () => {
        dialog.openAsync.mockResolvedValue(null);
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('.recipe-actions button')[1].click();
        await waitForAsyncTasksAsync();
        expect(dialog.openAsync).toHaveBeenCalledWith(expect.objectContaining({ returnUrl: '/explore/recipe', mode: 'login' }));
        expect(facade.saveAsync).not.toHaveBeenCalled();
    });
    it('continues saving after successful login', async () => {
        const closed = new Subject<unknown>();
        dialog.openAsync.mockResolvedValue({ afterClosed: () => closed.asObservable() });
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('.recipe-actions button')[1].click();
        await waitForAsyncTasksAsync();
        authenticated.set(true);
        closed.next(undefined);
        closed.complete();
        await waitForAsyncTasksAsync();
        await waitForAsyncTasksAsync();
        expect(facade.saveAsync).toHaveBeenCalledWith(expect.objectContaining({ id: 'recipe' }));
    });
    it('adds authenticated users directly to the diary', async () => {
        authenticated.set(true);
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]')?.click();
        harness.detectChanges();
        harness.routeNativeElement?.querySelector<HTMLButtonElement>('.recipe-actions button')?.click();
        await waitForAsyncTasksAsync();
        expect(facade.addToDiaryAsync).toHaveBeenCalledWith('recipe', SELECTED_DIARY_SERVINGS);
        expect(dialog.openAsync).not.toHaveBeenCalled();
    });
    it('keeps the hero serving count in sync and updates its label when the language changes', async () => {
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]')?.click();
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('.recipe-facts')?.textContent).toContain('3 PUBLIC_RECIPES.SERVINGS_OTHER');
        expect(harness.routeNativeElement?.querySelector('.nutrition-warning fd-ui-icon')).not.toBeNull();

        await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('.recipe-facts')?.textContent).toContain('3 PUBLIC_RECIPES.SERVINGS_FEW');
    });
});

describe('Shopping from successive visits to a public recipe', () => {
    const firstRecipeUrl = '/explore/recipe';
    const otherRecipeUrl = '/explore/other-recipe';
    const listUrl = environment.apiUrls.shoppingLists;
    let http: HttpTestingController | null;

    beforeEach(() => {
        http = null;
        provideRecipeShoppingOwner();
    });

    afterEach(() => {
        vi.unstubAllGlobals();
        http?.match(request => request.url.startsWith('./assets/i18n/'));
        http?.verify();
    });

    it.each(['choose', 'add'])('does not resume an old shopping %s after leaving and reopening a recipe', async action => {
        const loginClosed = new Subject<void>();
        dialog.openAsync.mockResolvedValueOnce({ afterClosed: () => loginClosed });
        const harness = await RouterTestingHarness.create(firstRecipeUrl);
        http = TestBed.inject(HttpTestingController);
        for (const translation of http.match(request => request.url.startsWith('./assets/i18n/'))) {
            translation.flush({});
        }

        shoppingAction(harness, action).click();
        await vi.waitFor(() => {
            expect(loginClosed.observed).toBe(true);
        });

        await harness.navigateByUrl(otherRecipeUrl);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Salad');
        await harness.navigateByUrl(firstRecipeUrl);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Soup');

        authenticated.set(true);
        loginClosed.next();
        loginClosed.complete();
        await harness.fixture.whenStable();
        await waitForAsyncTasksAsync();
        harness.detectChanges();

        http.expectNone(request => request.url.startsWith(listUrl), 'shopping destinations requested by the earlier recipe visit');
        expect(document.querySelector('fd-shopping-list-picker')).toBeNull();

        const fresh = shoppingAction(harness, action);
        expect(fresh.disabled).toBe(false);
        fresh.click();
        await waitForAsyncTasksAsync();
        const lookup = http.expectOne(request => request.method === 'GET' && request.url === `${listUrl}/page`);
        lookup.flush([]);
        await vi.dynamicImportSettled();
        const close = await vi.waitFor(() => {
            harness.detectChanges();
            const button = Array.from(document.querySelectorAll<HTMLButtonElement>('fd-shopping-list-picker button')).find(item =>
                item.textContent.includes('PUBLIC_RECIPES.SHOPPING_CANCEL'),
            );
            if (button === undefined) {
                throw new Error('Expected the shopping destination picker for a fresh action');
            }
            return button;
        });
        close.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(shoppingAction(harness, action).disabled).toBe(false);
        });
        http.expectNone(request => request.method !== 'GET');
    });
});

describe('Shopping picker owned by the public recipe', () => {
    const firstRecipeUrl = '/explore/recipe';
    const nextRecipeUrl = '/explore/other-recipe';
    const overviewUrl = '/explore';

    beforeEach(() => {
        authenticated.set(true);
        provideRecipeShoppingOwner();
    });

    afterEach(() => {
        TestBed.inject(FdUiDialogService).closeAll();
        vi.unstubAllGlobals();
        const http = TestBed.inject(HttpTestingController);
        http.match(request => request.url.startsWith('./assets/i18n/'));
        http.verify();
    });

    it.each([
        { destination: nextRecipeUrl, title: 'Salad' },
        { destination: overviewUrl, title: 'Soup' },
    ])('closes a mounted picker without writing when navigation opens $destination', async ({ destination, title }) => {
        const harness = await RouterTestingHarness.create(firstRecipeUrl);
        const http = TestBed.inject(HttpTestingController);
        for (const translation of http.match(request => request.url.startsWith('./assets/i18n/'))) {
            translation.flush({});
        }
        const originalPicker = await openShoppingIngredientPickerAsync(harness, http, 'Rice');

        await harness.navigateByUrl(destination);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(title);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(originalPicker.isConnected).toBe(false);
        });
        http.expectNone(request => request.method !== 'GET', 'shopping write for the closed picker');

        if (destination === nextRecipeUrl) {
            await openShoppingIngredientPickerAsync(harness, http, 'Lettuce');
        }
    });
});

describe('Shopping lookup across a reused public recipe page', () => {
    beforeEach(() => {
        authenticated.set(true);
        provideRecipeShoppingOwner();
    });

    afterEach(() => {
        TestBed.inject(FdUiDialogService).closeAll();
        vi.unstubAllGlobals();
        const http = TestBed.inject(HttpTestingController);
        http.match(request => request.url.startsWith('./assets/i18n/'));
        http.verify();
    });

    it('cancels the previous shopping lookup and keeps the replacement recipe actionable', async () => {
        const harness = await RouterTestingHarness.create('/explore/recipe');
        const http = TestBed.inject(HttpTestingController);
        for (const translation of http.match(request => request.url.startsWith('./assets/i18n/'))) {
            translation.flush({});
        }
        const firstPage = harness.routeNativeElement;
        const previous = shoppingAction(harness, 'add');
        expect(previous.disabled).toBe(false);
        previous.click();
        await waitForAsyncTasksAsync();

        const pending = new Set<TestRequest>();
        const previousLookup = holdShoppingLookup(http, pending);
        expect(previousLookup.cancelled).toBe(false);
        try {
            await harness.navigateByUrl('/explore/other-recipe');
            expect(harness.routeNativeElement).toBe(firstPage);
            expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Salad');
            expect(previousLookup.cancelled).toBe(true);
            pending.delete(previousLookup);
            const next = shoppingAction(harness, 'add');
            expect(next.disabled).toBe(false);
            next.click();
            await waitForAsyncTasksAsync();
            const nextLookup = holdShoppingLookup(http, pending);
            harness.detectChanges();
            expect(next.disabled).toBe(true);
            expect(harness.routeNativeElement?.querySelector('.shopping-message')?.textContent.trim()).toBe('');
            expect(document.querySelector('fd-shopping-list-picker')).toBeNull();

            nextLookup.flush([]);
            pending.delete(nextLookup);
            await vi.waitFor(() => {
                harness.detectChanges();
                expect(document.querySelector('fd-shopping-list-picker')?.textContent).toContain('Lettuce');
            });
            http.expectNone(request => request.method !== 'GET', 'no ingredient write before choosing its destination');
        } finally {
            for (const request of pending) {
                if (!request.cancelled) {
                    request.flush([]);
                }
            }
        }
    });
});

function holdShoppingLookup(http: HttpTestingController, pending: Set<TestRequest>): TestRequest {
    const lookup = http.expectOne(request => request.method === 'GET' && request.url === `${environment.apiUrls.shoppingLists}/page`);
    pending.add(lookup);
    return lookup;
}

function provideRecipeShoppingOwner(): void {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    TestBed.overrideComponent(PublicRecipeDetailComponent, {
        set: { providers: [{ provide: PublicRecipesFacade, useValue: facade }, ShoppingListAddFacade] },
    });
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    getById.mockImplementation(id => of(shoppingRecipe(id)));
}

async function openShoppingIngredientPickerAsync(
    harness: RouterTestingHarness,
    http: HttpTestingController,
    ingredient: string,
): Promise<HTMLElement> {
    const trigger = await vi.waitFor(() => {
        harness.detectChanges();
        const action = shoppingAction(harness, 'add');
        expect(action.disabled).toBe(false);
        return action;
    });
    trigger.click();
    await waitForAsyncTasksAsync();
    http.expectOne(request => request.method === 'GET' && request.url === `${environment.apiUrls.shoppingLists}/page`).flush([]);
    return vi.waitFor(() => {
        harness.detectChanges();
        const picker = document.querySelector<HTMLElement>('fd-shopping-list-picker');
        if (picker === null) {
            throw new Error('Expected the shopping destination dialog');
        }
        expect(picker.textContent).toContain(ingredient);
        return picker;
    });
}

function shoppingRecipe(id: RecipeId): PublicRecipe {
    const recipe = publicRecipeFixture();
    const first = id === 'recipe';
    const ingredient = {
        name: first ? 'Rice' : 'Lettuce',
        amount: null,
        unit: null,
        amountText: null,
        recipeId: null,
        isAvailable: true,
    };
    return {
        ...recipe,
        id,
        name: first ? recipe.name : 'Salad',
        steps: recipe.steps.map(step => ({ ...step, ingredients: [ingredient] })),
    };
}

function shoppingAction(harness: RouterTestingHarness, action: string): HTMLButtonElement {
    if (action === 'choose') {
        return recipeAction(harness, 'PUBLIC_RECIPES.SHOPPING_SELECT_LIST');
    }
    const button = harness.routeNativeElement?.querySelector<HTMLButtonElement>('button[aria-label="PUBLIC_RECIPES.SHOPPING_ADD"]');
    if (button === null || button === undefined) {
        throw new Error('Expected the native ingredient shopping button');
    }
    return button;
}
