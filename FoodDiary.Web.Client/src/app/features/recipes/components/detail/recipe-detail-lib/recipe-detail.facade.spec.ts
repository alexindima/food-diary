import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FavoriteRecipeService } from '../../../api/favorite-recipe.service';
import { RecipeService } from '../../../api/recipe.service';
import { type FavoriteRecipe, type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { RecipeDetailFacade } from './recipe-detail.facade';
import { RecipeDetailActionResult } from './recipe-detail.types';

const RECIPE_ID = 'recipe-1';
const DUPLICATED_RECIPE_ID = 'recipe-2';
const FAVORITE_ID = 'favorite-1';

let facade: RecipeDetailFacade;
let recipeService: { duplicate: ReturnType<typeof vi.fn> };
let favoriteRecipeService: {
    add: ReturnType<typeof vi.fn>;
    getAll: ReturnType<typeof vi.fn>;
    isFavorite: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
};
let dialogRef: { close: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(() => {
    recipeService = {
        duplicate: vi.fn().mockReturnValue(of(createRecipe({ id: DUPLICATED_RECIPE_ID }))),
    };
    favoriteRecipeService = {
        add: vi.fn().mockReturnValue(of(createFavoriteRecipe())),
        getAll: vi.fn().mockReturnValue(of([createFavoriteRecipe()])),
        isFavorite: vi.fn().mockReturnValue(of(false)),
        remove: vi.fn().mockReturnValue(of(null)),
    };
    dialogRef = {
        close: vi.fn(),
    };
    dialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(true) }),
    };

    TestBed.configureTestingModule({
        providers: [
            RecipeDetailFacade,
            { provide: RecipeService, useValue: recipeService },
            { provide: FavoriteRecipeService, useValue: favoriteRecipeService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FdUiDialogService, useValue: dialogService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
        ],
    });

    facade = TestBed.inject(RecipeDetailFacade);
});

describe('RecipeDetailFacade initialization and close', () => {
    it('initializes favorite state from API', () => {
        favoriteRecipeService.isFavorite.mockReturnValueOnce(of(true));

        facade.initialize(createRecipe({ isFavorite: false }));

        expect(favoriteRecipeService.isFavorite).toHaveBeenCalledWith(RECIPE_ID);
        expect(facade.isFavorite()).toBe(true);
    });

    it('closes without action when favorite state is unchanged', () => {
        facade.initialize(createRecipe({ isFavorite: false }));

        facade.close(createRecipe());

        expect(dialogRef.close).toHaveBeenCalledWith();
    });

    it('closes with FavoriteChanged action when favorite state changed', () => {
        facade.initialize(createRecipe({ isFavorite: false }));
        facade.toggleFavorite(createRecipe());

        facade.close(createRecipe());

        expect(dialogRef.close).toHaveBeenCalledWith(new RecipeDetailActionResult(RECIPE_ID, 'FavoriteChanged', true));
    });
});

describe('RecipeDetailFacade actions', () => {
    it('closes with edit action', () => {
        facade.initialize(createRecipe());

        facade.edit(createRecipe());

        expect(dialogRef.close).toHaveBeenCalledWith(new RecipeDetailActionResult(RECIPE_ID, 'Edit', false));
    });

    it('confirms before closing with delete action', () => {
        facade.initialize(createRecipe());

        facade.delete(createRecipe());

        expect(dialogService.open).toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledWith(new RecipeDetailActionResult(RECIPE_ID, 'Delete', false));
    });

    it('does not close with delete action when confirmation is cancelled', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(false) });
        facade.initialize(createRecipe());

        facade.delete(createRecipe());

        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('duplicates recipe and closes with duplicated id', () => {
        facade.initialize(createRecipe());

        facade.duplicate(createRecipe());

        expect(recipeService.duplicate).toHaveBeenCalledWith(RECIPE_ID);
        expect(dialogRef.close).toHaveBeenCalledWith(new RecipeDetailActionResult(DUPLICATED_RECIPE_ID, 'Duplicate', false));
    });

    it('keeps duplicate loading false after duplicate failure', () => {
        recipeService.duplicate.mockReturnValueOnce(throwError(() => new Error('duplicate failed')));
        facade.initialize(createRecipe());

        facade.duplicate(createRecipe());

        expect(facade.isDuplicateInProgress()).toBe(false);
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

describe('RecipeDetailFacade favorites', () => {
    it('adds recipe to favorites and stores favorite id', () => {
        facade.initialize(createRecipe({ isFavorite: false }));

        facade.toggleFavorite(createRecipe());

        expect(favoriteRecipeService.add).toHaveBeenCalledWith(RECIPE_ID);
        expect(facade.isFavorite()).toBe(true);
        expect(facade.isFavoriteLoading()).toBe(false);
    });

    it('removes favorite by existing favorite id', () => {
        favoriteRecipeService.isFavorite.mockReturnValueOnce(of(true));
        facade.initialize(createRecipe({ isFavorite: true, favoriteRecipeId: FAVORITE_ID }));

        facade.toggleFavorite(createRecipe({ isFavorite: true, favoriteRecipeId: FAVORITE_ID }));

        expect(favoriteRecipeService.remove).toHaveBeenCalledWith(FAVORITE_ID);
        expect(facade.isFavorite()).toBe(false);
        expect(facade.isFavoriteLoading()).toBe(false);
    });

    it('looks up favorite id before removing when recipe has no favorite id', () => {
        favoriteRecipeService.isFavorite.mockReturnValueOnce(of(true));
        facade.initialize(createRecipe({ isFavorite: true, favoriteRecipeId: null }));

        facade.toggleFavorite(createRecipe({ isFavorite: true, favoriteRecipeId: null }));

        expect(favoriteRecipeService.getAll).toHaveBeenCalled();
        expect(favoriteRecipeService.remove).toHaveBeenCalledWith(FAVORITE_ID);
    });

    it('ignores favorite toggle while loading', () => {
        const pendingAdd$ = new Subject<FavoriteRecipe>();
        favoriteRecipeService.add.mockReturnValueOnce(pendingAdd$);
        facade.initialize(createRecipe({ isFavorite: false }));

        facade.toggleFavorite(createRecipe());
        facade.toggleFavorite(createRecipe());

        expect(favoriteRecipeService.add).toHaveBeenCalledTimes(1);
    });
});

function createRecipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: RECIPE_ID,
        name: 'Recipe',
        servings: 1,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        isFavorite: false,
        favoriteRecipeId: null,
        steps: [],
        ...overrides,
    };
}

function createFavoriteRecipe(): FavoriteRecipe {
    return {
        id: FAVORITE_ID,
        recipeId: RECIPE_ID,
        name: 'Recipe',
        createdAtUtc: '2026-01-01T00:00:00Z',
        recipeName: 'Recipe',
        servings: 1,
        totalTimeMinutes: null,
        ingredientCount: 0,
    };
}
