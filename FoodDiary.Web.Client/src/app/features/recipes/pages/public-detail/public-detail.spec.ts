import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { PublicAuthDialogService } from '../../../public/contracts/auth-dialog';
import { ShoppingListAddFacade } from '../../../shopping-lists/contracts/shopping-list-add';
import { PublicRecipeService } from '../../api/public-recipe.service';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { publicRecipeResolver } from '../../resolvers/public-recipe.resolver';
import { PublicRecipeDetailComponent } from './public-detail';

const SELECTED_DIARY_SERVINGS = 3;

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
            provideRouter([
                {
                    path: 'explore',
                    component: PublicRecipeDetailComponent,
                    data: { seo: { recipe: publicRecipeFixture(), error: null } },
                },
                {
                    path: 'explore/:id',
                    component: PublicRecipeDetailComponent,
                    resolve: { seo: publicRecipeResolver },
                    runGuardsAndResolvers: 'always',
                },
            ]),
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
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('PUBLIC_RECIPES.ERROR');
        recipeAction(harness, 'PUBLIC_RECIPES.RETRY').click();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(getById.mock.calls).toEqual([['other-recipe'], ['other-recipe']]);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Stew');
        expect(harness.routeNativeElement?.querySelector('.recipe-facts')?.textContent).toContain('RECIPE_LANGUAGE.RU');
        expect(harness.routeNativeElement?.querySelector<HTMLElement>('.recipe-time-breakdown')?.hidden).toBe(true);
    });
});

function recipeAction(harness: RouterTestingHarness, label: string): HTMLButtonElement {
    const actions = harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('button');
    const selected = Array.from(actions ?? []).find(button => button.querySelector('.fd-ui-button__label')?.textContent.trim() === label);
    if (selected === undefined) {
        throw new Error(`Expected the recipe action ${label}`);
    }
    return selected;
}
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
    it('keeps the hero servings in sync with the ingredient control', async () => {
        const harness = await RouterTestingHarness.create('/explore/recipe');
        harness.routeNativeElement?.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]')?.click();
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('.recipe-facts')?.textContent).toContain('3 PUBLIC_RECIPES.SERVINGS_OTHER');
        expect(harness.routeNativeElement?.querySelector('.nutrition-warning fd-ui-icon')).not.toBeNull();
    });
});
