import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import type { FavoriteRecipe } from '../../../shared/models/recipe.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PublicAuthDialogService } from '../../public/contracts/auth-dialog';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { PublicCatalogFavorites } from './public-catalog-favorites.facade';
import { publicRecipeFixture } from './public-recipe.test-helper';

const authenticated = signal(false);
const account = signal('alice');
const api = { getLookupPage: vi.fn(), add: vi.fn(), remove: vi.fn() };
const dialog = { openAsync: vi.fn() };
let favorites: PublicCatalogFavorites;
beforeEach(() => {
    authenticated.set(false);
    account.set('alice');
    api.getLookupPage.mockReset().mockReturnValue(of([]));
    api.add.mockReset().mockReturnValue(of({ id: 'favorite-id' }));
    api.remove.mockReset().mockReturnValue(of(undefined));
    dialog.openAsync.mockReset().mockResolvedValue(null);
    TestBed.configureTestingModule({
        providers: [
            PublicCatalogFavorites,
            {
                provide: AuthService,
                useValue: { isAuthenticated: authenticated, getUserId: (): string | null => (authenticated() ? account() : null) },
            },
            { provide: FavoriteRecipeService, useValue: api },
            { provide: PublicAuthDialogService, useValue: dialog },
            { provide: Router, useValue: { url: '/explore?maxTotalTime=30' } },
        ],
    });
    favorites = TestBed.inject(PublicCatalogFavorites);
    TestBed.tick();
});
describe('PublicCatalogFavorites discovery recovery', () => {
    it('reports failed discovery and retries it before deciding whether to add or remove', async () => {
        api.getLookupPage.mockReturnValueOnce(throwError(() => new Error('offline')));
        authenticated.set(true);
        TestBed.tick();
        await vi.waitFor(() => {
            expect(favorites.message()).toBe('PUBLIC_RECIPES.ACTION_ERROR');
        });
        api.getLookupPage.mockReturnValueOnce(of([{ id: 'existing', recipeId: 'recipe' }]));
        await favorites.toggleAsync(publicRecipeFixture());
        expect(api.getLookupPage).toHaveBeenCalledTimes(2);
        expect(api.add).not.toHaveBeenCalled();
        expect(api.remove).toHaveBeenCalledExactlyOnceWith('existing');
        expect(favorites.savedIds().size).toBe(0);
    });
});

describe('PublicCatalogFavorites account replacement', () => {
    it('replaces the private saved map when the authenticated account changes', async () => {
        api.getLookupPage.mockReturnValueOnce(of([{ id: 'alice-favorite', recipeId: 'recipe' }]));
        authenticated.set(true);
        TestBed.tick();
        await vi.waitFor(() => {
            expect(favorites.savedIds().has('recipe')).toBe(true);
        });
        api.getLookupPage.mockReturnValueOnce(of([{ id: 'bob-favorite', recipeId: 'bob-recipe' }]));
        account.set('bob');
        TestBed.tick();
        await vi.waitFor(() => {
            expect([...favorites.savedIds()]).toEqual(['bob-recipe']);
        });
        await favorites.toggleAsync(publicRecipeFixture());
        expect(api.remove).not.toHaveBeenCalled();
        expect(api.add).toHaveBeenCalledExactlyOnceWith('recipe', 'Soup');
    });

    it('does not transfer an action awaiting the previous account lookup to its replacement', async () => {
        const lookup = new Subject<FavoriteRecipe[]>();
        api.getLookupPage.mockReturnValueOnce(lookup);
        authenticated.set(true);
        TestBed.tick();
        const action = favorites.toggleAsync(publicRecipeFixture());
        await vi.waitFor(() => {
            expect(favorites.busyIds().has('recipe')).toBe(true);
        });
        account.set('bob');
        TestBed.tick();
        lookup.next([]);
        lookup.complete();
        await action;
        expect(api.getLookupPage).toHaveBeenCalledTimes(2);
        expect(api.add).not.toHaveBeenCalled();
        expect(api.remove).not.toHaveBeenCalled();
        expect(favorites.message()).toBeNull();
    });

    it('keeps a replacement account action busy when the previous account request fails late', async () => {
        const previous = new Subject<FavoriteRecipe>();
        const replacement = new Subject<FavoriteRecipe>();
        api.add.mockReturnValueOnce(previous).mockReturnValueOnce(replacement);
        authenticated.set(true);
        TestBed.tick();
        const first = favorites.toggleAsync(publicRecipeFixture());
        await vi.waitFor(() => {
            expect(api.add).toHaveBeenCalledTimes(1);
        });
        account.set('bob');
        TestBed.tick();
        const second = favorites.toggleAsync(publicRecipeFixture());
        await vi.waitFor(() => {
            expect(api.add).toHaveBeenCalledTimes(2);
        });
        previous.error(new Error('old account offline'));
        await first;
        expect(favorites.message()).toBeNull();
        expect(favorites.busyIds().has('recipe')).toBe(true);
        replacement.error(new Error('current account offline'));
        await second;
        expect(favorites.message()).toBe('PUBLIC_RECIPES.ACTION_ERROR');
        expect(favorites.busyIds().size).toBe(0);
    });
});

describe('PublicCatalogFavorites login ownership', () => {
    it('keeps the authenticated owner when another account replaces it before the save continuation', async () => {
        const lookup = new Subject<FavoriteRecipe[]>();
        api.getLookupPage.mockReturnValue(lookup);
        dialog.openAsync.mockImplementation(() => {
            authenticated.set(true);
            TestBed.tick();
            return { afterClosed: (): Observable<boolean> => of(true) };
        });
        const action = favorites.toggleAsync(publicRecipeFixture());
        await vi.waitFor(() => {
            expect(api.getLookupPage).toHaveBeenCalledTimes(2);
        });
        lookup.next([]);
        lookup.complete();
        const replacement = firstValueFrom(of(undefined)).then(async () => {
            await firstValueFrom(of(undefined));
            api.getLookupPage.mockReturnValue(of([]));
            account.set('bob');
            TestBed.tick();
        });
        await Promise.all([action, replacement]);
        expect(account()).toBe('bob');
        expect(api.add).not.toHaveBeenCalled();
        expect(api.remove).not.toHaveBeenCalled();
        expect(favorites.message()).toBeNull();
    });

    it('does not save for a replacement account while the signed-in favorite lookup is pending', async () => {
        const lookup = new Subject<FavoriteRecipe[]>();
        api.getLookupPage.mockReturnValue(lookup);
        dialog.openAsync.mockImplementation(() => {
            authenticated.set(true);
            TestBed.tick();
            return { afterClosed: (): Observable<boolean> => of(true) };
        });
        const action = favorites.toggleAsync(publicRecipeFixture());
        await vi.waitFor(() => {
            expect(api.getLookupPage).toHaveBeenCalledTimes(2);
        });
        api.getLookupPage.mockReturnValueOnce(of([]));
        account.set('bob');
        TestBed.tick();
        lookup.next([]);
        lookup.complete();
        await action;
        expect(api.add).not.toHaveBeenCalled();
        expect(api.remove).not.toHaveBeenCalled();
        expect(favorites.message()).toBeNull();
    });
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
            pending.next({
                id: entityId<'favorite-recipe'>('late'),
                recipeId: entityId<'recipe'>('recipe'),
                recipeName: 'Soup',
                createdAtUtc: utcInstant(''),
                servings: 2,
                ingredientCount: 0,
            });
            pending.complete();
            await action;
            expect(favorites.savedIds().size).toBe(0);
        });
    });
});
