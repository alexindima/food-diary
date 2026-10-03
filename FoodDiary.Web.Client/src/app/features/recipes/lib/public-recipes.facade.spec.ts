import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { type Recipe, RecipeVisibility } from '../../../shared/models/recipe.data';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { PublicRecipeService } from '../api/public-recipe.service';
import { RecipeService } from '../api/recipe.service';
import { publicRecipeFixture } from './public-recipe.test-helper';
import { PublicRecipesFacade } from './public-recipes.facade';

const api = { query: vi.fn(), getCategories: vi.fn() };
const recipes = { getById: vi.fn() };
const favorites = { add: vi.fn(), isFavorite: vi.fn() };
const quickMeal = { addRecipe: vi.fn() };
let facade: PublicRecipesFacade;
beforeEach(() => {
    vi.resetAllMocks();
    TestBed.configureTestingModule({
        providers: [
            PublicRecipesFacade,
            { provide: PublicRecipeService, useValue: api },
            { provide: RecipeService, useValue: recipes },
            { provide: FavoriteRecipeService, useValue: favorites },
            { provide: QuickMealService, useValue: quickMeal },
        ],
    });
    facade = TestBed.inject(PublicRecipesFacade);
});

it('preserves public catalog filters, results and language-specific categories', async () => {
    const filters = { page: 2, search: 'Soup', category: 'soups', maxTotalTime: 30, sortBy: 'fastest', language: 'ru' };
    const page = { data: [publicRecipeFixture()], page: 2, limit: 20, totalPages: 2, totalItems: 21 };
    api.query.mockReturnValue(of(page));
    api.getCategories.mockReturnValue(of(['soups']));
    expect(await firstValueFrom(facade.query(filters))).toEqual(page);
    expect(api.query).toHaveBeenCalledWith(filters);
    expect(await firstValueFrom(facade.getCategories('Soup', 'ru'))).toEqual(['soups']);
    expect(api.getCategories).toHaveBeenCalledWith('Soup', 'ru');
});

it('reads the current favorite state and waits for the save request', async () => {
    favorites.isFavorite.mockReturnValue(of(true));
    expect(await firstValueFrom(facade.isFavorite('recipe'))).toBe(true);
    expect(favorites.isFavorite).toHaveBeenCalledWith('recipe');
    const saved = new Subject<void>();
    favorites.add.mockReturnValue(saved);
    const completed = vi.fn();
    const saving = facade.saveAsync(publicRecipeFixture()).then(completed);
    expect(favorites.add).toHaveBeenCalledWith('recipe', 'Soup');
    expect(completed).not.toHaveBeenCalled();
    saved.next();
    saved.complete();
    await saving;
    expect(completed).toHaveBeenCalledOnce();
});

it('propagates a failed favorite write', async () => {
    favorites.add.mockReturnValue(throwError(() => new Error('Favorite write failed')));
    await expect(facade.saveAsync(publicRecipeFixture())).rejects.toThrow('Favorite write failed');
});

it('loads the complete recipe before opening the quick meal flow', async () => {
    const recipe: Recipe = {
        id: 'recipe',
        name: 'Soup',
        servings: 2,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-09-01T00:00:00Z',
        isOwnedByCurrentUser: false,
        isNutritionAutoCalculated: true,
        steps: [],
    };
    const response = new Subject<Recipe>();
    recipes.getById.mockReturnValue(response);
    const adding = facade.addToDiaryAsync('recipe');
    expect(recipes.getById).toHaveBeenCalledWith('recipe');
    expect(quickMeal.addRecipe).not.toHaveBeenCalled();
    response.next(recipe);
    response.complete();
    await adding;
    expect(quickMeal.addRecipe).toHaveBeenCalledWith(recipe);
});

it('does not open the quick meal flow for an unavailable recipe', async () => {
    recipes.getById.mockReturnValue(of(null));
    await expect(facade.addToDiaryAsync('deleted')).rejects.toThrow('Recipe unavailable');
    expect(quickMeal.addRecipe).not.toHaveBeenCalled();
});

it('propagates recipe lookup errors without opening the quick meal flow', async () => {
    recipes.getById.mockReturnValue(throwError(() => new Error('Offline')));
    await expect(facade.addToDiaryAsync('recipe')).rejects.toThrow('Offline');
    expect(quickMeal.addRecipe).not.toHaveBeenCalled();
});
