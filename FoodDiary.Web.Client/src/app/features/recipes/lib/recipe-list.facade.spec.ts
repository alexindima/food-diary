import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { NavigationService } from '../../../services/navigation.service';
import { QuickMealService } from '../../meals/lib/quick-meal.service';
import { RecipeService } from '../api/recipe.service';
import { RecipeVisibility } from '../models/recipe.data';
import { RecipeListFacade } from './recipe-list.facade';

describe('RecipeListFacade', () => {
    let facade: RecipeListFacade;
    let recipeService: {
        queryWithRecent: ReturnType<typeof vi.fn>;
        deleteById: ReturnType<typeof vi.fn>;
    };
    let navigationService: {
        navigateToRecipeAdd: ReturnType<typeof vi.fn>;
        navigateToRecipeEdit: ReturnType<typeof vi.fn>;
    };
    let quickMealService: { addRecipe: ReturnType<typeof vi.fn> };
    let toastService: { open: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

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

    beforeEach(() => {
        recipeService = {
            queryWithRecent: vi.fn().mockReturnValue(
                of({
                    recentItems: [recipe],
                    allRecipes: {
                        data: [recipe],
                        page: 1,
                        limit: 10,
                        totalPages: 1,
                        totalItems: 1,
                    },
                }),
            ),
            deleteById: vi.fn().mockReturnValue(of(undefined)),
        };

        navigationService = {
            navigateToRecipeAdd: vi.fn().mockResolvedValue(true),
            navigateToRecipeEdit: vi.fn().mockResolvedValue(true),
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

    it('loads recipes and updates derived state', () => {
        facade.loadRecipes(1, 10, null, false).subscribe();

        expect(recipeService.queryWithRecent).toHaveBeenCalledWith(1, 10, { search: null }, true, 10);
        expect(facade.recipeData.items()).toEqual([recipe]);
        expect(facade.recentRecipes()).toEqual([recipe]);
        expect(facade.errorKey()).toBeNull();
        expect(facade.showRecentSection()).toBe(true);
    });

    it('sets load error state when query fails', () => {
        recipeService.queryWithRecent.mockReturnValueOnce(throwError(() => ({ status: 500 })));

        facade.loadRecipes(1, 10, ' soup ', false).subscribe();

        expect(facade.recipeData.items()).toEqual([]);
        expect(facade.recentRecipes()).toEqual([]);
        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
    });

    it('deletes recipe and reloads first page', () => {
        facade.deleteRecipe(recipe, 'soup', true).subscribe();

        expect(recipeService.deleteById).toHaveBeenCalledWith('recipe-1');
        expect(recipeService.queryWithRecent).toHaveBeenCalledWith(1, 10, { search: 'soup' }, false, 10);
        expect(facade.isDeleting()).toBe(false);
    });

    it('handles delete failure with toast', () => {
        recipeService.deleteById.mockReturnValueOnce(throwError(() => ({ status: 400 })));

        facade.deleteRecipe(recipe, null, false).subscribe();

        expect(toastService.error).toHaveBeenCalledWith('RECIPE_LIST.DELETE_ERROR');
        expect(facade.isDeleting()).toBe(false);
    });

    it('forwards add-to-meal to quick meal service', () => {
        facade.addToMeal(recipe);

        expect(quickMealService.addRecipe).toHaveBeenCalledWith(recipe);
    });
});
