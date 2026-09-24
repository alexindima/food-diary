import { HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { QuickMealService } from '../../meals/lib/quick/quick-meal.service';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { RecipeService } from '../api/recipe.service';
import { RecipeDetailActionResult } from '../components/detail/recipe-detail-lib/recipe-detail.types';
import { type FavoriteRecipe, RecipeVisibility } from '../models/recipe.data';
import { RecipeListFacade } from './recipe-list.facade';

const PAGE_LIMIT = 10;

const recipe = {
    id: 'recipe-1',
    name: 'Recipe',
    servings: 1,
    visibility: RecipeVisibility.Public,
    usageCount: 0,
    createdAt: '2026-04-02T00:00:00Z',
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
};

let facade: RecipeListFacade;
let recipeService: {
    queryOverview: ReturnType<typeof vi.fn>;
    query: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
};
let favoriteRecipeService: {
    getPage: ReturnType<typeof vi.fn>;
    getAll: ReturnType<typeof vi.fn>;
    add: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
};
let navigationService: {
    navigateToRecipeAddAsync: ReturnType<typeof vi.fn>;
    navigateToRecipeEditAsync: ReturnType<typeof vi.fn>;
};
let quickMealService: { addRecipe: ReturnType<typeof vi.fn> };
let toastService: { open: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

beforeEach(() => {
    recipeService = {
        queryOverview: vi.fn().mockReturnValue(
            of({
                recentItems: [recipe],
                favoriteItems: [],
                favoriteTotalCount: 0,
                allRecipes: {
                    data: [recipe],
                    page: 1,
                    limit: PAGE_LIMIT,
                    totalPages: 1,
                    totalItems: 1,
                },
            }),
        ),
        query: vi.fn().mockReturnValue(
            of({
                data: [recipe],
                page: 1,
                limit: PAGE_LIMIT,
                totalPages: 1,
                totalItems: 1,
            }),
        ),
        getById: vi.fn().mockReturnValue(of(recipe)),
        deleteById: vi.fn().mockReturnValue(of(void 0)),
    };

    favoriteRecipeService = {
        getPage: vi.fn().mockReturnValue(of({ data: [createFavoriteRecipe()], page: 1, limit: 1, totalItems: 1, totalPages: 1 })),
        getAll: vi.fn().mockReturnValue(of([createFavoriteRecipe()])),
        add: vi.fn().mockReturnValue(of(createFavoriteRecipe())),
        remove: vi.fn().mockReturnValue(of(null)),
    };

    navigationService = {
        navigateToRecipeAddAsync: vi.fn().mockResolvedValue(true),
        navigateToRecipeEditAsync: vi.fn().mockResolvedValue(true),
    };

    quickMealService = {
        addRecipe: vi.fn(),
    };

    toastService = {
        open: vi.fn(),
        error: vi.fn(),
    };

    TestBed.configureTestingModule({
        providers: [
            RecipeListFacade,
            { provide: RecipeService, useValue: recipeService },
            { provide: FavoriteRecipeService, useValue: favoriteRecipeService },
            { provide: NavigationService, useValue: navigationService },
            { provide: QuickMealService, useValue: quickMealService },
            { provide: FdUiToastService, useValue: toastService },
            { provide: FdUiDialogService, useValue: { open: vi.fn() } },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
        ],
    });

    facade = TestBed.inject(RecipeListFacade);
});

describe('RecipeListFacade favorites', () => {
    it('loads favorites and updates counters', () => {
        facade.loadFavorites().subscribe();

        expect(favoriteRecipeService.getPage).toHaveBeenCalledWith(1, 1);
        expect(facade.favoriteRecipes()).toEqual([]);
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(facade.isFavoritesLoadingMore()).toBe(false);
    });

    it('adds favorite recipe, syncs recipe state, and reloads favorites', () => {
        const notFavoriteRecipe = { ...recipe, isFavorite: false, favoriteRecipeId: null };
        facade.recipeData.setData({
            data: [notFavoriteRecipe],
            page: 1,
            limit: PAGE_LIMIT,
            totalPages: 1,
            totalItems: 1,
        });

        facade.toggleRecipeFavorite(notFavoriteRecipe).subscribe();

        expect(favoriteRecipeService.add).toHaveBeenCalledWith('recipe-1', 'Recipe');
        expect(favoriteRecipeService.getPage).toHaveBeenCalledWith(1, 1);
        expect(facade.recipeData.items()[0]).toEqual(expect.objectContaining({ isFavorite: true, favoriteRecipeId: 'favorite-1' }));
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });

    it('removes favorite recipe by looking up favorite id when recipe state has no favorite id', () => {
        const favoriteRecipe = { ...recipe, isFavorite: true, favoriteRecipeId: null };
        facade.recipeData.setData({
            data: [favoriteRecipe],
            page: 1,
            limit: PAGE_LIMIT,
            totalPages: 1,
            totalItems: 1,
        });

        facade.toggleRecipeFavorite(favoriteRecipe).subscribe();

        expect(favoriteRecipeService.getPage).toHaveBeenCalledWith(1, 1);
        expect(favoriteRecipeService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.recipeData.items()[0]).toEqual(expect.objectContaining({ isFavorite: false, favoriteRecipeId: null }));
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });

    it('removes favorite entry and syncs related recipe state', () => {
        const favorite = createFavoriteRecipe();
        facade.favoriteRecipes.set([favorite]);
        facade.favoriteTotalCount.set(1);
        facade.recipeData.setData({
            data: [{ ...recipe, isFavorite: true, favoriteRecipeId: favorite.id }],
            page: 1,
            limit: PAGE_LIMIT,
            totalPages: 1,
            totalItems: 1,
        });

        facade.removeFavorite(favorite).subscribe();

        expect(favoriteRecipeService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.favoriteRecipes()).toEqual([]);
        expect(facade.favoriteTotalCount()).toBe(0);
        expect(facade.recipeData.items()[0]).toEqual(expect.objectContaining({ isFavorite: false, favoriteRecipeId: null }));
    });
});

describe('RecipeListFacade overview', () => {
    it('loads initial overview and updates derived state', () => {
        facade.loadInitialOverview(1, PAGE_LIMIT, { search: null }, false).subscribe();

        expect(recipeService.queryOverview).toHaveBeenCalledWith({
            page: 1,
            limit: PAGE_LIMIT,
            filters: { search: null },
            includePublic: true,
            recentLimit: PAGE_LIMIT,
            favoriteLimit: 0,
        });
        expect(facade.recipeData.items()).toEqual([recipe]);
        expect(facade.recentRecipes()).toEqual([recipe]);
        expect(facade.errorKey()).toBeNull();
        expect(facade.showRecentSection()).toBe(true);
    });

    it('sets load error state when overview query fails', () => {
        recipeService.queryOverview.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.InternalServerError })));

        facade.loadInitialOverview(1, PAGE_LIMIT, { search: ' soup ' }, false).subscribe();

        expect(facade.recipeData.items()).toEqual([]);
        expect(facade.recentRecipes()).toEqual([]);
        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
    });
});

function createFavoriteRecipe(): FavoriteRecipe {
    return {
        id: 'favorite-1',
        recipeId: 'recipe-1',
        name: 'Recipe',
        createdAtUtc: '2026-04-02T00:00:00Z',
        recipeName: 'Recipe',
        servings: 1,
        totalTimeMinutes: 10,
        ingredientCount: 2,
    };
}

describe('RecipeListFacade actions', () => {
    it('deletes recipe and reloads first page', () => {
        facade.deleteRecipe(recipe, 'soup', true).subscribe();

        expect(recipeService.deleteById).toHaveBeenCalledWith('recipe-1');
        expect(recipeService.queryOverview).toHaveBeenCalledWith({
            page: 1,
            limit: PAGE_LIMIT,
            filters: { search: 'soup' },
            includePublic: false,
            recentLimit: 1,
            favoriteLimit: 0,
        });
        expect(facade.isDeleting()).toBe(false);
    });

    it('handles delete failure with toast', () => {
        recipeService.deleteById.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.BadRequest })));

        facade.deleteRecipe(recipe, null, false).subscribe();

        expect(toastService.error).toHaveBeenCalledWith('RECIPE_LIST.DELETE_ERROR');
        expect(facade.isDeleting()).toBe(false);
    });

    it('forwards add-to-meal to quick meal service', () => {
        facade.addToMeal(recipe);

        expect(quickMealService.addRecipe).toHaveBeenCalledWith(recipe);
    });

    it('handles detail add-to-meal action', async () => {
        await facade.handleDetailActionAsync(new RecipeDetailActionResult(recipe.id, 'AddToMeal'), recipe, null, false);

        expect(quickMealService.addRecipe).toHaveBeenCalledWith(recipe);
    });
});

describe('RecipeListFacade favorite picker', () => {
    it('restores the new server identity and uses it for the next removal', async () => {
        const favorite = createFavoriteRecipe();
        favoriteRecipeService.add.mockReturnValueOnce(of({ ...favorite, id: 'restored-id' }));
        facade.recentRecipes.set([{ ...recipe, isFavorite: false }]);
        expect(await firstValueFrom(facade.restorePickerFavorite(favorite))).toBe(true);
        expect(favorite.id).toBe('restored-id');
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(facade.recentRecipes()[0]).toMatchObject({ isFavorite: true, favoriteRecipeId: 'restored-id' });
        expect(await firstValueFrom(facade.removePickerFavorite(favorite))).toBe(true);
        expect(favoriteRecipeService.remove).toHaveBeenCalledWith('restored-id');
        expect(facade.favoriteTotalCount()).toBe(0);
    });

    it('keeps identity and counts intact when restore or removal fails', async () => {
        const favorite = createFavoriteRecipe();
        facade.favoriteTotalCount.set(1);
        favoriteRecipeService.add.mockReturnValueOnce(throwError(() => new Error('offline')));
        favoriteRecipeService.remove.mockReturnValueOnce(throwError(() => new Error('offline')));
        expect(await firstValueFrom(facade.restorePickerFavorite(favorite))).toBe(false);
        expect(await firstValueFrom(facade.removePickerFavorite(favorite))).toBe(false);
        expect(favorite.id).toBe('favorite-1');
        expect(facade.favoriteTotalCount()).toBe(1);
    });

    it('adds only an accessible recipe and permits retry after lookup failure', async () => {
        const favorite = createFavoriteRecipe();
        recipeService.getById.mockReturnValueOnce(of(null));
        expect(await firstValueFrom(facade.addFavoriteToMeal(favorite))).toBe(false);
        expect(quickMealService.addRecipe).not.toHaveBeenCalled();
        expect(await firstValueFrom(facade.addFavoriteToMeal(favorite))).toBe(true);
        expect(quickMealService.addRecipe).toHaveBeenCalledExactlyOnceWith(recipe);
    });
});

describe('RecipeListFacade competing requests', () => {
    it('cancels initial load when search starts and preserves newer results', () => {
        const initial = new Subject<unknown>();
        const search = new Subject<unknown>();
        recipeService.queryOverview.mockReturnValueOnce(initial).mockReturnValueOnce(search);
        facade.loadInitialOverview(1, PAGE_LIMIT, {}, false).subscribe();
        facade.loadRecipes(1, PAGE_LIMIT, { search: 'rice' }, false).subscribe();
        expect(initial.observed).toBe(false);
        search.next({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 2,
            allRecipes: { data: [{ ...recipe, name: 'Rice' }], page: 1, limit: PAGE_LIMIT, totalPages: 1, totalItems: 1 },
        });
        search.complete();
        initial.next({ allRecipes: { data: [recipe] } });
        expect(facade.recipeData.items()[0].name).toBe('Rice');
        expect(facade.favoriteTotalCount()).toBe(2);
    });

    it('cancels page requests on scope destruction', () => {
        const pending = new Subject<unknown>();
        recipeService.queryOverview.mockReturnValue(pending);
        facade.loadRecipes(2, PAGE_LIMIT, {}, true).subscribe();
        expect(pending.observed).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
    });
});

describe('RecipeListFacade edge cases', () => {
    it.each([false, true])('reports favorite mutation errors without changing state and permits retry: %s', async isFavorite => {
        const item = { ...recipe, isFavorite, favoriteRecipeId: 'favorite-1' };
        facade.recipeData.items.set([item]);
        const mutation = isFavorite ? favoriteRecipeService.remove : favoriteRecipeService.add;
        mutation.mockReturnValueOnce(throwError(() => new Error('offline')));
        await firstValueFrom(facade.toggleRecipeFavorite(item));
        expect(facade.recipeData.items()[0].isFavorite).toBe(isFavorite);
        expect(facade.favoriteLoadingIds().size).toBe(0);
        expect(toastService.error).toHaveBeenCalledTimes(1);
        await firstValueFrom(facade.toggleRecipeFavorite(item));
        expect(facade.recipeData.items()[0].isFavorite).toBe(!isFavorite);
    });

    it('blocks duplicate favorite mutations while one is pending', () => {
        const pending = new Subject<FavoriteRecipe>();
        favoriteRecipeService.add.mockReturnValue(pending);
        facade.toggleRecipeFavorite(recipe).subscribe();
        facade.toggleRecipeFavorite(recipe).subscribe();
        expect(favoriteRecipeService.add).toHaveBeenCalledTimes(1);
        pending.next(createFavoriteRecipe());
        pending.complete();
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });

    it.each([{ isOwnedByCurrentUser: false }, { usageCount: 1 }])('does not delete a protected recipe: %s', async overrides => {
        await firstValueFrom(facade.deleteRecipe({ ...recipe, ...overrides }, null, false));
        expect(recipeService.deleteById).not.toHaveBeenCalled();
    });

    it('keeps recent recipes separate and exposes search results after search', () => {
        facade.loadInitialOverview(1, PAGE_LIMIT, {}, false).subscribe();
        expect(facade.showRecentSection()).toBe(true);
        expect(facade.allRecipesSectionItems()).toEqual([]);
        expect(facade.hasVisibleRecipes()).toBe(true);
        expect(facade.allRecipesSectionLabelKey()).toBe('RECIPE_LIST.ALL_RECIPES');
        facade.loadRecipes(1, PAGE_LIMIT, { search: 'rice' }, false).subscribe();
        expect(facade.showRecentSection()).toBe(false);
        expect(facade.allRecipesSectionItems()).toEqual([recipe]);
        expect(facade.allRecipesSectionLabelKey()).toBe('RECIPE_LIST.SEARCH_RESULTS');
    });

    it('clears stale results on a page error and recovers on retry', () => {
        recipeService.queryOverview.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.loadRecipes(1, PAGE_LIMIT, {}, false).subscribe();
        expect(facade.hasVisibleRecipes()).toBe(false);
        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
        facade.loadRecipes(1, PAGE_LIMIT, {}, false).subscribe();
        expect(facade.errorKey()).toBeNull();
        expect(facade.hasVisibleRecipes()).toBe(true);
    });

    it.each([{ category: 'Soup' }, { maxTotalTime: 0 }, { caloriesFrom: 0 }, { caloriesTo: 0 }, { hasImage: false }])(
        'preserves meaningful falsy filters: %s',
        filters => {
            expect(facade.hasActiveFilters(false, filters)).toBe(true);
        },
    );

    it('distinguishes blank search and filters from active ones', () => {
        expect(facade.hasActiveFilters(false, { category: '  ' })).toBe(false);
        expect(facade.hasActiveFilters(true, {})).toBe(true);
        expect(facade.hasSearch(null)).toBe(false);
        expect(facade.hasSearch(' ')).toBe(false);
        expect(facade.hasSearch('Rice')).toBe(true);
    });

    it.each(['Edit', 'Duplicate'] as const)('routes the %s result to the correct recipe', async action => {
        await facade.handleDetailActionAsync(new RecipeDetailActionResult('new-id', action), recipe, null, false);
        expect(navigationService.navigateToRecipeEditAsync).toHaveBeenCalledWith(action === 'Edit' ? recipe.id : 'new-id');
    });
});

describe('RecipeListFacade recovery and navigation', () => {
    it('preserves favorite count after refresh failure and releases loading state', async () => {
        facade.favoriteTotalCount.set(2);
        favoriteRecipeService.getPage.mockReturnValueOnce(throwError(() => new Error('offline')));
        await firstValueFrom(facade.loadFavorites());
        expect(facade.favoriteTotalCount()).toBe(2);
        expect(facade.isFavoritesLoadingMore()).toBe(false);
    });

    it('does not add a meal when recipe resolution fails', async () => {
        recipeService.getById.mockReturnValueOnce(throwError(() => new Error('offline')));
        expect(await firstValueFrom(facade.addFavoriteToMeal(createFavoriteRecipe()))).toBe(false);
        expect(quickMealService.addRecipe).not.toHaveBeenCalled();
        expect(await firstValueFrom(facade.getFavoriteRecipe(createFavoriteRecipe()))).toEqual(recipe);
    });

    it('opens filters with current values and returns cancellation unchanged', async () => {
        const dialogs = TestBed.inject(FdUiDialogService);
        const dialog = { afterClosed: (): Observable<null> => of(null) };
        const open = vi.spyOn(dialogs, 'open').mockReturnValue(dialog as ReturnType<FdUiDialogService['open']>);
        const filters = { onlyMine: true, category: 'Soup', maxTotalTime: null, caloriesFrom: null, caloriesTo: null, hasImage: false };
        expect(await firstValueFrom(facade.openFilters(filters))).toBeNull();
        expect(open).toHaveBeenCalledWith(expect.anything(), { preset: 'form', data: filters });
        await facade.navigateToAddRecipeAsync();
        expect(navigationService.navigateToRecipeAddAsync).toHaveBeenCalledTimes(1);
    });
});
