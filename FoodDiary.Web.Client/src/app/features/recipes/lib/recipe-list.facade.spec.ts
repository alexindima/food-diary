import { HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { QuickMealService } from '../../meals/lib/quick/quick-meal.service';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { RecipeService } from '../api/recipe.service';
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
let favoriteRecipeService: { getAll: ReturnType<typeof vi.fn>; add: ReturnType<typeof vi.fn>; remove: ReturnType<typeof vi.fn> };
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
        deleteById: vi.fn().mockReturnValue(of(undefined)),
    };

    favoriteRecipeService = {
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

        expect(favoriteRecipeService.getAll).toHaveBeenCalled();
        expect(facade.favoriteRecipes()).toEqual([createFavoriteRecipe()]);
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
        expect(favoriteRecipeService.getAll).toHaveBeenCalled();
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

        expect(favoriteRecipeService.getAll).toHaveBeenCalled();
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
        facade.loadInitialOverview(1, PAGE_LIMIT, null, false).subscribe();

        expect(recipeService.queryOverview).toHaveBeenCalledWith({
            page: 1,
            limit: PAGE_LIMIT,
            filters: { search: null },
            includePublic: true,
            recentLimit: PAGE_LIMIT,
            favoriteLimit: PAGE_LIMIT,
        });
        expect(facade.recipeData.items()).toEqual([recipe]);
        expect(facade.recentRecipes()).toEqual([recipe]);
        expect(facade.errorKey()).toBeNull();
        expect(facade.showRecentSection()).toBe(true);
    });

    it('sets load error state when overview query fails', () => {
        recipeService.queryOverview.mockReturnValueOnce(throwError(() => ({ status: HttpStatusCode.InternalServerError })));

        facade.loadInitialOverview(1, PAGE_LIMIT, ' soup ', false).subscribe();

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
        expect(recipeService.query).toHaveBeenCalledWith(1, PAGE_LIMIT, { search: 'soup' }, false);
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
});
