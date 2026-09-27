import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { PublicAuthDialogService } from '../../../public/lib/public-auth-dialog.service';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { PublicRecipeDetailComponent } from './public-detail';

describe('public recipe account actions', () => {
    const authenticated = signal(false);
    const facade = { isFavorite: vi.fn(() => of(false)), saveAsync: vi.fn(), addToDiaryAsync: vi.fn() };
    const dialog = { openAsync: vi.fn() };
    beforeEach(() => {
        authenticated.set(false);
        vi.clearAllMocks();
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
            ],
        });
        TestBed.overrideComponent(PublicRecipeDetailComponent, {
            set: { providers: [{ provide: PublicRecipesFacade, useValue: facade }] },
        });
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
});
