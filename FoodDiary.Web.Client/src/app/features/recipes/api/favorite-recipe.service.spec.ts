import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import type { FavoriteRecipe } from '../../../shared/models/recipe.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
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

describe('FavoriteRecipeService lookup pagination', () => {
    it('collects favorites beyond the first lookup page and emits only the complete list', () => {
        const firstPage = Array.from({ length: 100 }, (_, index) => ({
            ...createFavoriteRecipe(),
            id: entityId<'favorite-recipe'>(`favorite-${index.toString()}`),
            recipeId: entityId<'recipe'>(`recipe-${index.toString()}`),
        }));
        const lastFavorite = {
            ...createFavoriteRecipe(),
            id: entityId<'favorite-recipe'>('last-favorite'),
            recipeId: entityId<'recipe'>('last-recipe'),
        };
        const next = vi.fn();
        service.getLookupPage().subscribe(next);

        httpMock
            .expectOne(req => req.url === `${BASE_URL}/page` && req.params.get('page') === '1' && req.params.get('limit') === '100')
            .flush({ data: firstPage, page: 1, limit: 100, totalPages: 2, totalItems: 101 });
        expect(next).not.toHaveBeenCalled();

        httpMock
            .expectOne(req => req.url === `${BASE_URL}/page` && req.params.get('page') === '2' && req.params.get('limit') === '100')
            .flush({ data: [lastFavorite], page: 2, limit: 100, totalPages: 2, totalItems: 101 });
        expect(next).toHaveBeenCalledExactlyOnceWith([...firstPage, lastFavorite]);
    });

    it('does not expose a partial lookup when a later favorites page fails', () => {
        const next = vi.fn();
        service.getLookupPage().subscribe(next);
        httpMock
            .expectOne(req => req.url === `${BASE_URL}/page` && req.params.get('page') === '1')
            .flush({ data: [createFavoriteRecipe()], page: 1, limit: 100, totalPages: 2, totalItems: 101 });
        expect(next).not.toHaveBeenCalled();
        httpMock
            .expectOne(req => req.url === `${BASE_URL}/page` && req.params.get('page') === '2')
            .flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        expect(next).toHaveBeenCalledExactlyOnceWith([]);
    });
});

describe('FavoriteRecipeService', () => {
    it('requests a trimmed search and preserves pagination and nutrition metadata', () => {
        const data = [{ ...createFavoriteRecipe(), totalFiber: 8, ingredientNames: ['Rice'] }];
        const expected = { data, page: 2, limit: 10, totalItems: 12, totalPages: 2 };
        service.getPage(2, undefined, '  Rice  ').subscribe(result => {
            expect(result).toEqual(expected);
        });
        const request = httpMock.expectOne(req => req.url === `${BASE_URL}/page`);
        expect(request.request.params.get('page')).toBe('2');
        expect(request.request.params.get('limit')).toBe('10');
        expect(request.request.params.get('search')).toBe('Rice');
        request.flush(expected);
    });

    it('propagates page errors instead of presenting an empty favorites list', () => {
        let failed = false;
        service.getPage(1).subscribe({
            next: () => {
                throw new Error('An HTTP failure must not become an empty page');
            },
            error: () => {
                failed = true;
            },
        });
        httpMock
            .expectOne(req => req.url === `${BASE_URL}/page`)
            .flush('Unavailable', { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        expect(failed).toBe(true);
    });

    it('gets all favorite recipes', () => {
        const favorites = [createFavoriteRecipe()];

        service.getLookupPage().subscribe(result => {
            expect(result).toEqual(favorites);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/page` && request.params.get('limit') === '100');
        expect(req.request.method).toBe('GET');
        req.flush({ data: favorites, page: 1, limit: 100, totalPages: 1, totalItems: 1 });
    });

    it('returns an empty list when get all fails', () => {
        service.getLookupPage().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/page` && request.params.get('limit') === '100');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('checks favorite state', () => {
        service.isFavorite(entityId<'recipe'>('recipe-1')).subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/recipe-1`);
        expect(req.request.method).toBe('GET');
        req.flush(true);
    });

    it('returns false when favorite check fails', () => {
        service.isFavorite(entityId<'recipe'>('recipe-1')).subscribe(result => {
            expect(result).toBe(false);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/recipe-1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('adds favorite recipe with optional name', () => {
        const favorite = createFavoriteRecipe();

        service.add(entityId<'recipe'>('recipe-1'), 'Soup').subscribe(result => {
            expect(result).toEqual(favorite);
        });

        const req = httpMock.expectOne(BASE_URL);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ recipeId: 'recipe-1', name: 'Soup' });
        req.flush(favorite);
    });

    it('removes favorite recipe', () => {
        service.remove(entityId<'favorite-recipe'>('favorite-1')).subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/favorite-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

function createFavoriteRecipe(): FavoriteRecipe {
    return {
        id: entityId<'favorite-recipe'>('favorite-1'),
        recipeId: entityId<'recipe'>('recipe-1'),
        name: 'Soup',
        createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
        recipeName: 'Soup',
        imageUrl: null,
        totalCalories: TOTAL_CALORIES,
        servings: RECIPE_SERVINGS,
        totalTimeMinutes: TOTAL_TIME_MINUTES,
        ingredientCount: INGREDIENT_COUNT,
    };
}

describe('FavoriteRecipeService mutation failures', () => {
    it.each(['add', 'remove'] as const)('propagates %s failure', operation => {
        let status = 0;
        const request: Observable<unknown> =
            operation === 'add' ? service.add(entityId<'recipe'>('recipe-1')) : service.remove(entityId<'favorite-recipe'>('favorite-1'));
        request.subscribe({
            next: () => {
                throw new Error('Must not succeed');
            },
            error: (error: unknown) => {
                status = error instanceof HttpErrorResponse ? error.status : 0;
            },
        });
        httpMock
            .expectOne(req => req.method === (operation === 'add' ? 'POST' : 'DELETE'))
            .flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        expect(status).toBe(HttpStatusCode.ServiceUnavailable);
    });
});
