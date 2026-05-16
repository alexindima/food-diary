import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { PageOf } from '../../../shared/models/page-of.data';
import { type Recipe, RecipeVisibility } from '../../recipes/models/recipe.data';
import type { ExploreRecipe } from '../models/explore.data';
import { ExploreService } from './explore.service';

const BASE_URL = `${environment.apiUrls.recipes}/explore`;
const PAGE = 2;
const LIMIT = 20;
const TOTAL_ITEMS = 1;
const TOTAL_PAGES = 1;
const PREP_TIME = 30;
const RECIPE_SERVINGS = 2;
const RECIPE_CALORIES = 350;

let service: ExploreService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [ExploreService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ExploreService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('ExploreService query', () => {
    it('queries explore recipes with pagination and filters', () => {
        const page = createPage();

        service
            .query(PAGE, LIMIT, {
                search: '  soup  ',
                category: 'Dinner',
                maxPrepTime: PREP_TIME,
                sortBy: 'popular',
            })
            .subscribe(result => {
                expect(result).toEqual(page);
            });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/` && request.method === 'GET');
        expect(req.request.params.get('page')).toBe(String(PAGE));
        expect(req.request.params.get('limit')).toBe(String(LIMIT));
        expect(req.request.params.get('search')).toBe('soup');
        expect(req.request.params.get('category')).toBe('Dinner');
        expect(req.request.params.get('maxPrepTime')).toBe(String(PREP_TIME));
        expect(req.request.params.get('sortBy')).toBe('popular');
        req.flush(page);
    });

    it('omits blank optional filters', () => {
        service.query(PAGE, LIMIT, { search: '   ' }).subscribe();

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/` && request.method === 'GET');
        expect(req.request.params.has('search')).toBe(false);
        req.flush(createPage());
    });

    it('returns empty page on query failure', () => {
        service.query(PAGE, LIMIT).subscribe(result => {
            expect(result).toEqual({ data: [], page: PAGE, limit: LIMIT, totalPages: 0, totalItems: 0 });
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/` && request.method === 'GET');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Server Error' });
    });
});

function createPage(): PageOf<ExploreRecipe> {
    return {
        data: [createRecipe()],
        page: PAGE,
        limit: LIMIT,
        totalPages: TOTAL_PAGES,
        totalItems: TOTAL_ITEMS,
    };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Soup',
        description: null,
        comment: null,
        category: 'Dinner',
        imageUrl: null,
        imageAssetId: null,
        prepTime: PREP_TIME,
        cookTime: null,
        servings: RECIPE_SERVINGS,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-05-16T10:00:00.000Z',
        isOwnedByCurrentUser: false,
        totalCalories: RECIPE_CALORIES,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
