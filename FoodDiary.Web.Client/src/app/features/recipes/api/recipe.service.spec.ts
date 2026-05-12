import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { PageOf } from '../../../shared/models/page-of.data';
import { type Recipe, type RecipeDto, RecipeVisibility } from '../models/recipe.data';
import { RecipeService } from './recipe.service';

const BASE_URL = 'http://localhost:5300/api/v1/recipes';
const RECIPE_SERVINGS = 2;
const RECIPE_TOTAL_CALORIES = 350;
const RECIPE_TOTAL_PROTEINS = 40;
const RECIPE_TOTAL_FATS = 12;
const RECIPE_TOTAL_CARBS = 15;
const RECIPE_TOTAL_FIBER = 4;
const DEFAULT_PAGE_LIMIT = 10;
const RECENT_RECIPES_LIMIT = 5;
const NEW_RECIPE_PREP_MINUTES = 10;
const NEW_RECIPE_COOK_MINUTES = 20;
const NEW_RECIPE_SERVINGS = 4;
const HTTP_INTERNAL_SERVER_ERROR: number = HttpStatusCode.InternalServerError;
const HTTP_NOT_FOUND: number = HttpStatusCode.NotFound;
const MOCK_RECIPE: Recipe = {
    id: 'r1',
    name: 'Grilled Chicken Salad',
    description: null,
    comment: null,
    category: null,
    imageUrl: null,
    imageAssetId: null,
    prepTime: null,
    cookTime: null,
    servings: RECIPE_SERVINGS,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '2026-01-01',
    isOwnedByCurrentUser: true,
    totalCalories: RECIPE_TOTAL_CALORIES,
    totalProteins: RECIPE_TOTAL_PROTEINS,
    totalFats: RECIPE_TOTAL_FATS,
    totalCarbs: RECIPE_TOTAL_CARBS,
    totalFiber: RECIPE_TOTAL_FIBER,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    steps: [],
};
const MOCK_PAGE: PageOf<Recipe> = {
    data: [MOCK_RECIPE],
    page: 1,
    limit: DEFAULT_PAGE_LIMIT,
    totalPages: 1,
    totalItems: 1,
};

let service: RecipeService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [RecipeService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RecipeService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('RecipeService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});

describe('RecipeService query', () => {
    it('should query recipes with pagination params', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual(MOCK_PAGE);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_PAGE_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(MOCK_PAGE);
    });

    it('should include search filter in query params', () => {
        const filters = { search: 'salad' };

        service.query(1, DEFAULT_PAGE_LIMIT, filters, false).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('salad');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(MOCK_PAGE);
    });

    it('should return empty PageOf on query failure', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual({ data: [], page: 1, limit: DEFAULT_PAGE_LIMIT, totalPages: 0, totalItems: 0 });
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        req.flush('Server Error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });
});

describe('RecipeService reads', () => {
    it('should get recipe by id with includePublic param', () => {
        service.getById('r1').subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/r1` && r.method === 'GET');
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(MOCK_RECIPE);
    });

    it('should get recipe by id with includePublic false', () => {
        service.getById('r1', false).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/r1` && r.method === 'GET');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(MOCK_RECIPE);
    });

    it('should return null on getById failure', () => {
        service.getById('r1').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/r1` && r.method === 'GET');
        req.flush('Not Found', { status: HTTP_NOT_FOUND, statusText: 'Not Found' });
    });
});

describe('RecipeService mutations', () => {
    it('should create recipe', () => {
        const createData = createRecipeDto('New Recipe');

        service.create(createData).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(MOCK_RECIPE);
    });

    it('should update recipe via PATCH', () => {
        const updateData = createRecipeDto('Updated Recipe');

        service.update('r1', updateData).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/r1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(MOCK_RECIPE);
    });

    it('should delete recipe by id', () => {
        service.deleteById('r1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/r1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should duplicate recipe', () => {
        service.duplicate('r1').subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/r1/duplicate`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({});
        req.flush(MOCK_RECIPE);
    });
});

describe('RecipeService recent', () => {
    it('should get recent recipes with default params', () => {
        const recentRecipes = [MOCK_RECIPE];

        service.getRecent().subscribe(result => {
            expect(result).toEqual(recentRecipes);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_PAGE_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(recentRecipes);
    });

    it('should get recent recipes with custom params', () => {
        service.getRecent(RECENT_RECIPES_LIMIT, false).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(RECENT_RECIPES_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush([]);
    });

    it('should return empty array on getRecent failure', () => {
        service.getRecent().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        req.flush('Server Error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });
});

function createRecipeDto(name: string): RecipeDto {
    return {
        name,
        prepTime: NEW_RECIPE_PREP_MINUTES,
        cookTime: NEW_RECIPE_COOK_MINUTES,
        servings: NEW_RECIPE_SERVINGS,
        visibility: RecipeVisibility.Private,
        calculateNutritionAutomatically: true,
        steps: [],
    };
}
