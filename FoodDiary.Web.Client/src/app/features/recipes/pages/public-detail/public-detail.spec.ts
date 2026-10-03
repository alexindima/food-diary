import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { PublicAuthDialogService } from '../../../public/contracts/auth-dialog';
import { ShoppingListAddFacade } from '../../../shopping-lists/contracts/shopping-list-add';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { PublicRecipeDetailComponent } from './public-detail';

const authenticated = signal(false);
const facade = { isFavorite: vi.fn(() => of(false)), saveAsync: vi.fn(), addToDiaryAsync: vi.fn() };
const dialog = { openAsync: vi.fn() };
const browser = { getOrigin: vi.fn<() => string | undefined>(), copyTextAsync: vi.fn() };
beforeEach(() => {
    authenticated.set(false);
    vi.clearAllMocks();
    browser.getOrigin.mockReturnValue('https://fooddiary.test');
    browser.copyTextAsync.mockResolvedValue(undefined);
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
                    data: { seo: { recipe: publicRecipeFixture(), error: null } },
                },
            ]),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: PublicAuthDialogService, useValue: dialog },
            { provide: BrowserWindowService, useValue: browser },
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
        harness.routeNativeElement?.querySelector<HTMLButtonElement>('.recipe-actions button')?.click();
        await waitForAsyncTasksAsync();
        expect(facade.addToDiaryAsync).toHaveBeenCalledWith('recipe');
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
