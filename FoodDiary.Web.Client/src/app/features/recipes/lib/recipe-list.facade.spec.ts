import { HttpContext, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { type FavoriteRecipe, type RecipeOverview, RecipeVisibility } from '../../../shared/models/recipe.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { RecipeService } from '../api/recipe.service';
import { RecipeDetailActionResult } from '../components/detail/recipe-detail-lib/recipe-detail.types';
import { RecipeListFacade } from './recipe-list.facade';

const PAGE_LIMIT = 10;

const recipe = {
    id: entityId<'recipe'>('recipe-1'),
    name: 'Recipe',
    servings: 1,
    visibility: RecipeVisibility.Public,
    usageCount: 0,
    createdAt: utcInstant('2026-04-02T00:00:00Z'),
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
    getLookupPage: ReturnType<typeof vi.fn>;
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
        getLookupPage: vi.fn().mockReturnValue(of([createFavoriteRecipe()])),
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
            data: [{ ...recipe, isFavorite: true, favoriteRecipeId: favorite.id, id: recipe.id, createdAt: utcInstant(recipe.createdAt) }],
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

it('reconciles a stale favorite marker without deleting the unrelated saved recipe returned by lookup', async () => {
    const otherFavorite: FavoriteRecipe = {
        ...createFavoriteRecipe(),
        id: entityId<'favorite-recipe'>('favorite-2'),
        recipeId: entityId<'recipe'>('recipe-2'),
    };
    const staleRecipe = { ...recipe, isFavorite: true, favoriteRecipeId: null };
    const otherRecipe = { ...recipe, id: otherFavorite.recipeId, isFavorite: true, favoriteRecipeId: otherFavorite.id };
    const items = [staleRecipe, otherRecipe];
    const overview: RecipeOverview = {
        recentItems: items,
        favoriteItems: [otherFavorite],
        favoriteTotalCount: 1,
        allRecipes: { data: items, page: 1, limit: PAGE_LIMIT, totalPages: 1, totalItems: items.length },
    };
    recipeService.queryOverview.mockReturnValueOnce(of(overview));
    favoriteRecipeService.getLookupPage.mockReturnValueOnce(of([otherFavorite]));
    favoriteRecipeService.getPage.mockReturnValueOnce(of({ data: [otherFavorite], page: 1, limit: 1, totalItems: 1, totalPages: 1 }));
    await firstValueFrom(facade.loadInitialOverview(1, PAGE_LIMIT, {}, false));

    await firstValueFrom(facade.toggleRecipeFavorite(staleRecipe));

    expect(favoriteRecipeService.getLookupPage).toHaveBeenCalledOnce();
    expect(favoriteRecipeService.remove).not.toHaveBeenCalled();
    expect(favoriteRecipeService.getPage).toHaveBeenCalledExactlyOnceWith(1, 1);
    const expectedRecipes = [{ ...staleRecipe, isFavorite: false }, otherRecipe];
    expect(facade.recipeData.items()).toEqual(expectedRecipes);
    expect(facade.recentRecipes()).toEqual(expectedRecipes);
    expect(facade.favoriteTotalCount()).toBe(1);
    expect(facade.favoriteLoadingIds().size).toBe(0);
    expect(toastService.error).not.toHaveBeenCalled();
});

describe('RecipeListFacade overview', () => {
    it('opens a visible public recipe without requiring ownership', async () => {
        const publicRecipe = { ...recipe, isOwnedByCurrentUser: false };
        const request = new AbortController();
        recipeService.getById.mockImplementation((_id: string, includePublic: boolean) => of(includePublic ? publicRecipe : null));

        expect(await facade.getRecipeDetailAsync(publicRecipe.id, request.signal)).toEqual(publicRecipe);
        expect(recipeService.getById).toHaveBeenCalledWith(publicRecipe.id, true);
        expect(toastService.error).not.toHaveBeenCalled();
    });

    it.each(['absent', 'error'])('reports unavailable detail (%s) and allows another opening attempt', async failure => {
        const request = new AbortController();
        recipeService.getById.mockReturnValueOnce(failure === 'absent' ? of(null) : throwError(() => new Error('offline')));

        expect(await facade.getRecipeDetailAsync(recipe.id, request.signal)).toBeNull();
        expect(toastService.error).toHaveBeenCalledExactlyOnceWith('ERRORS.LOAD_FAILED_MESSAGE');
        expect(await facade.getRecipeDetailAsync(recipe.id, request.signal)).toEqual(recipe);
        expect(toastService.error).toHaveBeenCalledTimes(1);
    });

    it('loads initial overview and updates derived state', () => {
        facade.loadInitialOverview(1, PAGE_LIMIT, { search: null }, false).subscribe();

        expect(recipeService.queryOverview).toHaveBeenCalledWith(
            {
                page: 1,
                limit: PAGE_LIMIT,
                filters: { search: null },
                includePublic: true,
                recentLimit: 5,
                favoriteLimit: 0,
            },
            expect.any(HttpContext),
        );
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
        id: entityId<'favorite-recipe'>('favorite-1'),
        recipeId: entityId<'recipe'>('recipe-1'),
        name: 'Recipe',
        createdAtUtc: utcInstant('2026-04-02T00:00:00Z'),
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
        expect(recipeService.queryOverview).toHaveBeenCalledWith(
            {
                page: 1,
                limit: PAGE_LIMIT,
                filters: { search: 'soup' },
                includePublic: false,
                recentLimit: 1,
                favoriteLimit: 0,
            },
            expect.any(HttpContext),
        );
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

    it('deletes a recipe selected in details and keeps the current collection search', async () => {
        await facade.handleDetailActionAsync(new RecipeDetailActionResult(recipe.id, 'Delete'), recipe, 'Rice', false);

        expect(recipeService.deleteById).toHaveBeenCalledExactlyOnceWith(recipe.id);
        expect(recipeService.queryOverview).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ page: 1, filters: { search: 'Rice' }, includePublic: true }),
            expect.any(HttpContext),
        );
        expect(facade.currentPageIndex()).toBe(0);
    });
});

describe('RecipeListFacade favorite picker', () => {
    it('restores the new server identity and uses it for the next removal', async () => {
        const favorite = createFavoriteRecipe();
        favoriteRecipeService.add.mockReturnValueOnce(of({ ...favorite, id: entityId<'favorite-recipe'>('restored-id') }));
        facade.recentRecipes.set([{ ...recipe, isFavorite: false, id: recipe.id, createdAt: utcInstant(recipe.createdAt) }]);
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

    it('restores a favorite without a custom label and synchronizes the matching recipe only', async () => {
        const favorite = { ...createFavoriteRecipe(), name: null };
        const anotherRecipe = { ...recipe, id: entityId<'recipe'>('another-recipe') };
        facade.recipeData.items.set([recipe, anotherRecipe]);
        facade.recentRecipes.set([anotherRecipe, recipe]);

        expect(await firstValueFrom(facade.restorePickerFavorite(favorite))).toBe(true);

        expect(favoriteRecipeService.add).toHaveBeenCalledExactlyOnceWith(recipe.id, undefined);
        const restoredRecipe = { ...recipe, isFavorite: true, favoriteRecipeId: favorite.id };
        expect(facade.recipeData.items()).toEqual([restoredRecipe, anotherRecipe]);
        expect(facade.recentRecipes()).toEqual([anotherRecipe, restoredRecipe]);
        expect(facade.favoriteTotalCount()).toBe(1);
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
        const item = { ...recipe, isFavorite, favoriteRecipeId: entityId<'favorite-recipe'>('favorite-1') };
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
        await firstValueFrom(
            facade.deleteRecipe({ ...recipe, ...overrides, id: recipe.id, createdAt: utcInstant(recipe.createdAt) }, null, false),
        );
        expect(recipeService.deleteById).not.toHaveBeenCalled();
    });

    it('preserves recent recipes in the catalogue and exposes search results after search', () => {
        facade.loadInitialOverview(1, PAGE_LIMIT, {}, false).subscribe();
        expect(facade.showRecentSection()).toBe(true);
        expect(facade.allRecipesSectionItems()).toEqual([recipe]);
        expect(facade.hasVisibleRecipes()).toBe(true);
        expect(facade.allRecipesSectionLabelKey()).toBe('RECIPE_LIST.ALL_RECIPES');
        facade.loadRecipes(1, PAGE_LIMIT, { search: 'rice' }, false).subscribe();
        expect(facade.showRecentSection()).toBe(false);
        expect(facade.allRecipesSectionItems()).toEqual([recipe]);
        expect(facade.allRecipesSectionLabelKey()).toBe('RECIPE_LIST.SEARCH_RESULTS');
    });

    it('shows shortcuts only on the default first page and restores them after filtering', () => {
        facade.loadInitialOverview(1, PAGE_LIMIT, {}, false).subscribe();
        facade.currentPageIndex.set(1);
        expect(facade.showRecentSection()).toBe(false);
        facade.loadRecipes(1, PAGE_LIMIT, { hasImage: false }, false).subscribe();
        expect(facade.showRecentSection()).toBe(false);
        facade.loadInitialOverview(1, PAGE_LIMIT, {}, true).subscribe();
        expect(facade.showRecentSection()).toBe(false);
        facade.loadRecipes(1, PAGE_LIMIT, {}, false).subscribe();
        expect(facade.showRecentSection()).toBe(true);
        expect(facade.allRecipesSectionItems()).toEqual([recipe]);
        expect(recipeService.queryOverview).toHaveBeenLastCalledWith(expect.objectContaining({ recentLimit: 5 }), expect.any(HttpContext));
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
    it('recovers to the last available page after the catalogue shrinks', () => {
        const stalePage = 3;
        recipeService.queryOverview
            .mockReturnValueOnce(
                of({
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                    allRecipes: { data: [], page: stalePage, limit: PAGE_LIMIT, totalPages: 2, totalItems: 11 },
                }),
            )
            .mockReturnValueOnce(
                of({
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                    allRecipes: { data: [recipe], page: 2, limit: PAGE_LIMIT, totalPages: 2, totalItems: 11 },
                }),
            );

        facade.loadRecipes(stalePage, PAGE_LIMIT, { search: 'Rice' }, false).subscribe();
        TestBed.tick();

        expect(recipeService.queryOverview).toHaveBeenNthCalledWith(
            1,
            expect.objectContaining({ page: stalePage }),
            expect.any(HttpContext),
        );
        expect(recipeService.queryOverview).toHaveBeenNthCalledWith(2, expect.objectContaining({ page: 2 }), expect.any(HttpContext));
        expect(recipeService.queryOverview).toHaveBeenCalledTimes(2);
        expect(facade.currentPageIndex()).toBe(1);
        expect(facade.recipeData.items()).toEqual([recipe]);
        expect(facade.recipeData.isLoading()).toBe(false);
    });

    it('retains the requested later page after loading fails', () => {
        recipeService.queryOverview.mockReturnValueOnce(throwError(() => new Error('offline')));

        facade.loadRecipes(2, PAGE_LIMIT, {}, false).subscribe();

        expect(facade.currentPageIndex()).toBe(1);
        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
        expect(facade.recipeData.isLoading()).toBe(false);
    });

    it.each([false, true])('resets the retry page when a new first-page query fails: filtered=%s', filtered => {
        facade.currentPageIndex.set(1);
        recipeService.queryOverview.mockReturnValueOnce(throwError(() => new Error('offline')));

        facade.loadRecipes(1, PAGE_LIMIT, filtered ? { search: 'Rice', hasImage: false } : {}, filtered).subscribe();

        expect(facade.currentPageIndex()).toBe(0);
        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
    });

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
