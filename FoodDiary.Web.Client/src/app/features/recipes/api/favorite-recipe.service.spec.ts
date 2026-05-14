import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { FavoriteRecipe } from '../models/recipe.data';
import { FavoriteRecipeService } from './favorite-recipe.service';

const BASE_URL = 'http://localhost:5300/api/v1/favorite-recipes';
const RECIPE_SERVINGS = 2;
const TOTAL_CALORIES = 320;
const TOTAL_TIME_MINUTES = 45;
const INGREDIENT_COUNT = 4;

let service: FavoriteRecipeService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [FavoriteRecipeService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FavoriteRecipeService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('FavoriteRecipeService', () => {
    it('gets all favorite recipes', () => {
        const favorites = [createFavoriteRecipe()];

        service.getAll().subscribe(result => {
            expect(result).toEqual(favorites);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush(favorites);
    });

    it('returns an empty list when get all fails', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('checks favorite state', () => {
        service.isFavorite('recipe-1').subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/recipe-1`);
        expect(req.request.method).toBe('GET');
        req.flush(true);
    });

    it('returns false when favorite check fails', () => {
        service.isFavorite('recipe-1').subscribe(result => {
            expect(result).toBe(false);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/recipe-1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('adds favorite recipe with optional name', () => {
        const favorite = createFavoriteRecipe();

        service.add('recipe-1', 'Soup').subscribe(result => {
            expect(result).toEqual(favorite);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ recipeId: 'recipe-1', name: 'Soup' });
        req.flush(favorite);
    });

    it('removes favorite recipe', () => {
        service.remove('favorite-1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/favorite-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

function createFavoriteRecipe(): FavoriteRecipe {
    return {
        id: 'favorite-1',
        recipeId: 'recipe-1',
        name: 'Soup',
        createdAtUtc: '2026-01-01T00:00:00Z',
        recipeName: 'Soup',
        imageUrl: null,
        totalCalories: TOTAL_CALORIES,
        servings: RECIPE_SERVINGS,
        totalTimeMinutes: TOTAL_TIME_MINUTES,
        ingredientCount: INGREDIENT_COUNT,
    };
}
