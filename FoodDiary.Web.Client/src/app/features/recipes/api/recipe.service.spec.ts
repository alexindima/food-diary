import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { RecipeHttpResponse } from '../../../shared/api/sdk/generated/model/recipe-http-response';
import type { PageOf } from '../../../shared/models/page-of.data';
import { type Recipe, type RecipeDto, RecipeVisibility } from '../../../shared/models/recipe.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { RecipeSelectFacade } from '../lib/recipe-select.facade';
import { RecipeService } from './recipe.service';
import { RECIPE_API_LIMITS } from './recipe-api.tokens';

const BASE_URL = 'http://localhost:5300/api/v1/recipes';
const RECIPE_SERVINGS = 2;
const RECIPE_TOTAL_CALORIES = 350;
const RECIPE_TOTAL_PROTEINS = 40;
const RECIPE_TOTAL_FATS = 12;
const RECIPE_TOTAL_CARBS = 15;
const RECIPE_TOTAL_FIBER = 4;
const DEFAULT_PAGE_LIMIT = 10;
const DEFAULT_RECENT_LIMIT = 10;
const DEFAULT_OVERVIEW_RECENT_LIMIT = 10;
const DEFAULT_OVERVIEW_FAVORITE_LIMIT = 10;
const RECENT_RECIPES_LIMIT = 5;
const OVERRIDE_RECENT_LIMIT = 7;
const OVERRIDE_OVERVIEW_RECENT_LIMIT = 8;
const OVERRIDE_OVERVIEW_FAVORITE_LIMIT = 9;
const NEW_RECIPE_PREP_MINUTES = 10;
const NEW_RECIPE_COOK_MINUTES = 20;
const NEW_RECIPE_SERVINGS = 4;
const MOCK_RECIPE: Recipe = {
    id: entityId<'recipe'>('r1'),
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
    createdAt: utcInstant('2026-01-01'),
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
const PUBLICATION_RECIPE_RESPONSE = {
    id: MOCK_RECIPE.id,
    name: MOCK_RECIPE.name,
    servings: MOCK_RECIPE.servings,
    visibility: MOCK_RECIPE.visibility,
    usageCount: MOCK_RECIPE.usageCount,
    createdAt: MOCK_RECIPE.createdAt,
    isOwnedByCurrentUser: MOCK_RECIPE.isOwnedByCurrentUser,
    isNutritionAutoCalculated: MOCK_RECIPE.isNutritionAutoCalculated,
    steps: [],
} satisfies RecipeHttpResponse;
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

        const req = httpMock.expectOne(r => r.url === BASE_URL && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_RECENT_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(MOCK_PAGE);
    });

    it('should include search filter in query params', () => {
        const filters = { search: 'salad' };

        service.query(1, DEFAULT_PAGE_LIMIT, filters, false).subscribe();

        const req = httpMock.expectOne(r => r.url === BASE_URL && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('salad');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(MOCK_PAGE);
    });

    it('should return empty PageOf on query failure', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual({ data: [], page: 1, limit: DEFAULT_PAGE_LIMIT, totalPages: 0, totalItems: 0 });
        });

        const req = httpMock.expectOne(r => r.url === BASE_URL && r.method === 'GET');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('propagates HTTP failures to the recipe picker through its facade', async () => {
        const filters = { search: 'salad', category: 'Dinner' };
        const pending = firstValueFrom(TestBed.inject(RecipeSelectFacade).query(1, DEFAULT_PAGE_LIMIT, filters, false));
        const req = httpMock.expectOne(r => r.url === BASE_URL && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('salad');
        expect(req.request.params.get('category')).toBe('Dinner');
        expect(req.request.params.get('includePublic')).toBe('false');

        req.flush('Service Unavailable', { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.ServiceUnavailable });
    });
});

describe('RecipeService reads', () => {
    it('should get recipe by id with includePublic param', () => {
        service.getById(entityId<'recipe'>('r1')).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/r1` && r.method === 'GET');
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(MOCK_RECIPE);
    });

    it('should get recipe by id with includePublic false', () => {
        service.getById(entityId<'recipe'>('r1'), false).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/r1` && r.method === 'GET');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(MOCK_RECIPE);
    });

    it.each(['ChangesRequested', null] as const)(
        'preserves the publication status %s returned by the recipe API',
        async publicationStatus => {
            const result = firstValueFrom(service.getById(MOCK_RECIPE.id, false));
            const request = httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/${MOCK_RECIPE.id}?includePublic=false` });
            const response: RecipeHttpResponse = { ...PUBLICATION_RECIPE_RESPONSE, publicationStatus };
            request.flush(response);

            await expect(result).resolves.toMatchObject({ id: MOCK_RECIPE.id, publicationStatus });
        },
    );

    it('returns an unavailable recipe for an unsupported publication status', async () => {
        const result = firstValueFrom(service.getById(MOCK_RECIPE.id, false));
        const request = httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/${MOCK_RECIPE.id}?includePublic=false` });
        const response: RecipeHttpResponse = { ...PUBLICATION_RECIPE_RESPONSE, publicationStatus: 'Unknown' };
        request.flush(response);

        await expect(result).resolves.toBeNull();
    });

    it('should return null on getById failure', () => {
        service.getById(entityId<'recipe'>('r1')).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/r1` && r.method === 'GET');
        req.flush('Not Found', { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    });
});

it('loads a private cooked recipe with ordered galleries and distinct ingredient quantities', async () => {
    const id = entityId<'recipe'>('00000000-0000-4000-8000-000000000100');
    const productId = '00000000-0000-4000-8000-000000000200';
    const nestedRecipeId = '00000000-0000-4000-8000-000000000300';
    const recipePhotos = [
        { imageAssetId: '00000000-0000-4000-8000-000000000401', imageUrl: '/recipes/salad-cover.jpg' },
        { imageAssetId: '00000000-0000-4000-8000-000000000402', imageUrl: '/recipes/salad-served.jpg' },
    ];
    const stepPhotos = [
        { imageAssetId: '00000000-0000-4000-8000-000000000501', imageUrl: '/recipes/chicken-grilling.jpg' },
        { imageAssetId: '00000000-0000-4000-8000-000000000502', imageUrl: '/recipes/chicken-grilled.jpg' },
    ];
    const response: RecipeHttpResponse = {
        ...MOCK_RECIPE,
        id,
        category: 'salads',
        cookTime: NEW_RECIPE_COOK_MINUTES,
        imageUrl: recipePhotos[0].imageUrl,
        imageAssetId: recipePhotos[0].imageAssetId,
        images: recipePhotos,
        qualityGrade: 'yellow',
        qualityScore: 62,
        steps: [
            {
                id: '00000000-0000-4000-8000-000000000600',
                stepNumber: 1,
                title: 'Grill the chicken',
                instruction: 'Grill the chicken, then toss with dressing and salt.',
                imageUrl: stepPhotos[0].imageUrl,
                imageAssetId: stepPhotos[0].imageAssetId,
                images: stepPhotos,
                ingredients: [
                    {
                        id: '00000000-0000-4000-8000-000000000601',
                        productId,
                        productName: 'Chicken',
                        amount: 180,
                        productBaseUnit: 'G',
                    },
                    {
                        id: '00000000-0000-4000-8000-000000000602',
                        nestedRecipeId,
                        nestedRecipeName: 'Dressing',
                        nestedRecipeServings: 4,
                        amount: 1.5,
                    },
                    { id: '00000000-0000-4000-8000-000000000603', textName: 'Salt', amount: 0, amountText: 'to taste' },
                ],
            },
        ],
    };
    const result = firstValueFrom(service.getById(id, false));
    const request = httpMock.expectOne(req => req.url === `${BASE_URL}/${id}` && req.method === 'GET');
    expect(request.request.params.get('includePublic')).toBe('false');
    request.flush(response);

    await expect(result).resolves.toMatchObject({
        id,
        category: 'salads',
        visibility: RecipeVisibility.Private,
        qualityGrade: 'yellow',
        cookTime: NEW_RECIPE_COOK_MINUTES,
        imageAssetId: recipePhotos[0].imageAssetId,
        images: recipePhotos,
        steps: [
            {
                imageAssetId: stepPhotos[0].imageAssetId,
                images: stepPhotos,
                ingredients: [
                    { kind: 'product', productId, amount: 180, productBaseUnit: 'G' },
                    { kind: 'recipe', nestedRecipeId, amount: 1.5, nestedRecipeServings: 4 },
                    { kind: 'text', textName: 'Salt', amount: 0, amountText: 'to taste' },
                ],
            },
        ],
    });
});

describe('RecipeService mutations', () => {
    it('should create recipe', () => {
        const createData = createRecipeDto('New Recipe');

        service.create(createData).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(BASE_URL);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(MOCK_RECIPE);
    });

    it('should update recipe via PATCH', () => {
        const updateData = createRecipeDto('Updated Recipe');

        service.update(entityId<'recipe'>('r1'), updateData).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/r1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual({ ...updateData, clearPrepTime: false, clearCookTime: false });
        req.flush(MOCK_RECIPE);
    });

    it('should delete recipe by id', () => {
        service.deleteById(entityId<'recipe'>('r1')).subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/r1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should duplicate recipe', () => {
        service.duplicate(entityId<'recipe'>('r1')).subscribe(result => {
            expect(result).toEqual(MOCK_RECIPE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/r1/duplicate`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toBeNull();
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
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('RecipeService defaults', () => {
    it('should use injected overview limits by default', () => {
        service.queryOverview({ page: 1, limit: DEFAULT_PAGE_LIMIT }).subscribe(result => {
            expect(result.recentItems).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/overview` && r.method === 'GET');
        expect(req.request.params.get('recentLimit')).toBe(String(DEFAULT_OVERVIEW_RECENT_LIMIT));
        expect(req.request.params.get('favoriteLimit')).toBe(String(DEFAULT_OVERVIEW_FAVORITE_LIMIT));
        req.flush({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allRecipes: MOCK_PAGE,
        });
    });

    it('should allow recipe API limits to be overridden through DI', () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                RecipeService,
                provideHttpClient(),
                provideHttpClientTesting(),
                {
                    provide: RECIPE_API_LIMITS,
                    useValue: {
                        recent: OVERRIDE_RECENT_LIMIT,
                        overviewRecent: OVERRIDE_OVERVIEW_RECENT_LIMIT,
                        overviewFavorite: OVERRIDE_OVERVIEW_FAVORITE_LIMIT,
                    },
                },
            ],
        });
        service = TestBed.inject(RecipeService);
        httpMock = TestBed.inject(HttpTestingController);

        service.getRecent().subscribe();
        const recentReq = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        expect(recentReq.request.params.get('limit')).toBe(String(OVERRIDE_RECENT_LIMIT));
        recentReq.flush([]);

        service.queryOverview({ page: 1, limit: DEFAULT_PAGE_LIMIT }).subscribe();
        const overviewReq = httpMock.expectOne(r => r.url === `${BASE_URL}/overview` && r.method === 'GET');
        expect(overviewReq.request.params.get('recentLimit')).toBe(String(OVERRIDE_OVERVIEW_RECENT_LIMIT));
        expect(overviewReq.request.params.get('favoriteLimit')).toBe(String(OVERRIDE_OVERVIEW_FAVORITE_LIMIT));
        overviewReq.flush({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allRecipes: MOCK_PAGE,
        });
    });
});

describe('RecipeService overview', () => {
    it('should query overview with trimmed search, includePublic flag, and custom limits', () => {
        service
            .queryOverview({
                page: 1,
                limit: DEFAULT_PAGE_LIMIT,
                filters: { search: ' salad ' },
                includePublic: false,
                recentLimit: OVERRIDE_OVERVIEW_RECENT_LIMIT,
                favoriteLimit: OVERRIDE_OVERVIEW_FAVORITE_LIMIT,
            })
            .subscribe(result => {
                expect(result.allRecipes).toEqual(MOCK_PAGE);
            });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/overview` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_PAGE_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('false');
        expect(req.request.params.get('recentLimit')).toBe(String(OVERRIDE_OVERVIEW_RECENT_LIMIT));
        expect(req.request.params.get('favoriteLimit')).toBe(String(OVERRIDE_OVERVIEW_FAVORITE_LIMIT));
        expect(req.request.params.get('search')).toBe('salad');
        req.flush({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allRecipes: MOCK_PAGE,
        });
    });

    it('omits blank overview search and propagates failures to the page error state', () => {
        let failed = false;
        service.queryOverview({ page: 1, limit: DEFAULT_PAGE_LIMIT, filters: { search: '   ' } }).subscribe({
            next: () => {
                throw new Error('Failed overview must not be an empty result');
            },
            error: () => {
                failed = true;
            },
        });
        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/overview` && r.method === 'GET');
        expect(req.request.params.has('search')).toBe(false);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
        expect(failed).toBe(true);
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

describe('RecipeService mutation failures and filters', () => {
    it.each(['create', 'update', 'delete', 'duplicate'] as const)('propagates %s errors to the caller', operation => {
        const requests = {
            create: (): Observable<unknown> => service.create(createRecipeDto('Rice')),
            update: (): Observable<unknown> => service.update(entityId<'recipe'>('r1'), createRecipeDto('Rice')),
            delete: (): Observable<unknown> => service.deleteById(entityId<'recipe'>('r1')),
            duplicate: (): Observable<unknown> => service.duplicate(entityId<'recipe'>('r1')),
        };
        let status = 0;
        const request: Observable<unknown> = requests[operation]();
        request.subscribe({
            next: () => {
                throw new Error('Must not succeed');
            },
            error: (error: unknown) => {
                status = error instanceof HttpErrorResponse ? error.status : 0;
            },
        });
        httpMock
            .expectOne(req => req.url.startsWith(BASE_URL))
            .flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        expect(status).toBe(HttpStatusCode.ServiceUnavailable);
    });

    it('sends structured filters including false and zero without dropping them', () => {
        service
            .queryOverview({
                page: 1,
                limit: DEFAULT_PAGE_LIMIT,
                includePublic: false,
                filters: { category: '  Soup  ', maxTotalTime: 0, caloriesFrom: 0, caloriesTo: 120, hasImage: false },
            })
            .subscribe();
        const request = httpMock.expectOne(req => req.url.endsWith('/overview'));
        expect(request.request.params.get('category')).toBe('Soup');
        expect(request.request.params.get('maxTotalTime')).toBe('0');
        expect(request.request.params.get('caloriesFrom')).toBe('0');
        expect(request.request.params.get('caloriesTo')).toBe('120');
        expect(request.request.params.get('hasImage')).toBe('false');
        expect(request.request.params.get('includePublic')).toBe('false');
        request.flush({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allRecipes: { data: [], page: 1, limit: 10, totalPages: 0, totalItems: 0 },
        });
    });
});
