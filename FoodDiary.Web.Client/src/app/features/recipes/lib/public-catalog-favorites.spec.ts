import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { PublicAuthDialogService } from '../../public/lib/public-auth-dialog.service';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import type { FavoriteRecipe } from '../models/recipe.data';
import { PublicCatalogFavorites } from './public-catalog-favorites.facade';
import { publicRecipeFixture } from './public-recipe.test-helper';

const authenticated = signal(false);
const api = { getLookupPage: vi.fn(), add: vi.fn(), remove: vi.fn() };
const dialog = { openAsync: vi.fn() };
let favorites: PublicCatalogFavorites;
beforeEach(() => {
    authenticated.set(false);
    api.getLookupPage.mockReset().mockReturnValue(of([]));
    api.add.mockReset().mockReturnValue(of({ id: 'favorite-id' }));
    api.remove.mockReset().mockReturnValue(of(undefined));
    dialog.openAsync.mockReset().mockResolvedValue(null);
    TestBed.configureTestingModule({
        providers: [
            PublicCatalogFavorites,
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: FavoriteRecipeService, useValue: api },
            { provide: PublicAuthDialogService, useValue: dialog },
            { provide: Router, useValue: { url: '/explore?maxTotalTime=30' } },
        ],
    });
    favorites = TestBed.inject(PublicCatalogFavorites);
    TestBed.tick();
});
describe('PublicCatalogFavorites', () => {
    it('does not request private favorites for guests and preserves the return URL on login', async () => {
        await favorites.toggleAsync(publicRecipeFixture());
        expect(api.getLookupPage).not.toHaveBeenCalled();
        expect(api.add).not.toHaveBeenCalled();
        expect(dialog.openAsync).toHaveBeenCalledWith(expect.objectContaining({ returnUrl: '/explore?maxTotalTime=30' }));
    });
    it('continues saving after login and then removes using the favorite ID', async () => {
        dialog.openAsync.mockImplementation(() => {
            authenticated.set(true);
            TestBed.tick();
            return { afterClosed: (): Observable<boolean> => of(true) };
        });
        await favorites.toggleAsync(publicRecipeFixture());
        expect(api.add).toHaveBeenCalledWith('recipe', 'Soup');
        expect(favorites.savedIds().has('recipe')).toBe(true);
        await favorites.toggleAsync(publicRecipeFixture());
        expect(api.remove).toHaveBeenCalledWith('favorite-id');
        expect(favorites.savedIds().size).toBe(0);
    });
    it('loads existing favorites once and clears them on logout', async () => {
        api.getLookupPage.mockReturnValue(of([{ id: 'existing', recipeId: 'recipe' }]));
        authenticated.set(true);
        TestBed.tick();
        await vi.waitFor(() => {
            expect(favorites.savedIds().has('recipe')).toBe(true);
        });
        expect(api.getLookupPage).toHaveBeenCalledTimes(1);
        authenticated.set(false);
        TestBed.tick();
        expect(favorites.savedIds().size).toBe(0);
    });
    it('preserves the saved state when removal fails', async () => {
        authenticated.set(true);
        TestBed.tick();
        await favorites.toggleAsync(publicRecipeFixture());
        api.remove.mockReturnValue(throwError(() => new Error('offline')));
        await favorites.toggleAsync(publicRecipeFixture());
        expect(favorites.savedIds().has('recipe')).toBe(true);
        expect(favorites.message()).toBe('PUBLIC_RECIPES.ACTION_ERROR');
    });
    describe('concurrent operations', () => {
        it('prevents duplicate writes and ignores a response after logout', async () => {
            authenticated.set(true);
            TestBed.tick();
            const pending = new Subject<FavoriteRecipe>();
            api.add.mockReturnValue(pending);
            const action = favorites.toggleAsync(publicRecipeFixture());
            await vi.waitFor(() => {
                expect(api.add).toHaveBeenCalledTimes(1);
            });
            await favorites.toggleAsync(publicRecipeFixture());
            expect(api.add).toHaveBeenCalledTimes(1);
            authenticated.set(false);
            TestBed.tick();
            pending.next({ id: 'late', recipeId: 'recipe', recipeName: 'Soup', createdAtUtc: '', servings: 2, ingredientCount: 0 });
            pending.complete();
            await action;
            expect(favorites.savedIds().size).toBe(0);
        });
    });
});
