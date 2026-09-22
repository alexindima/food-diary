import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { getNumberProperty } from '../../../shared/lib/unknown-value.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import { type MealFilters, type MealItemResponseDto, type MealManageDto, type MealResponseDto, MealSourceType } from '../models/meal.data';
import { MealService } from './meal.service';

const BASE_URL = 'http://localhost:5300/api/v1/meals';
const DEFAULT_PAGE = 1;
const DEFAULT_LIMIT = 10;
const TOTAL_CALORIES = 500;
const PRE_MEAL_SATIETY_LEVEL = 3;
const POST_MEAL_SATIETY_LEVEL = 4;
const AI_TOTAL_CALORIES = 319.2;
const AI_TOTAL_PROTEINS = 9.9;
const AI_TOTAL_FATS = 4.8;
const AI_TOTAL_CARBS = 67.09;
const AI_TOTAL_FIBER = 13.79;
const MANUAL_TOTAL_CALORIES = 350;
const MOCK_MEAL_DTO: MealResponseDto = {
    id: 'm1',
    date: '2026-03-28',
    mealType: 'Lunch',
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    totalCalories: TOTAL_CALORIES,
    totalProteins: 40,
    totalFats: 20,
    totalCarbs: 30,
    totalFiber: 5,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    manualCalories: null,
    manualProteins: null,
    manualFats: null,
    manualCarbs: null,
    manualFiber: null,
    manualAlcohol: null,
    preMealSatietyLevel: PRE_MEAL_SATIETY_LEVEL,
    postMealSatietyLevel: POST_MEAL_SATIETY_LEVEL,
    items: [],
    aiSessions: [],
};
const MOCK_PAGE_DTO: PageOf<MealResponseDto> = {
    data: [MOCK_MEAL_DTO],
    page: DEFAULT_PAGE,
    limit: DEFAULT_LIMIT,
    totalPages: 1,
    totalItems: 1,
};
const DEFAULT_FILTERS: MealFilters = {
    dateFrom: '2026-03-01',
    dateTo: '2026-03-31',
};

let service: MealService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [MealService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MealService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('MealService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});

describe('MealService query', () => {
    it('should query meals with pagination and filters', () => {
        service.query(DEFAULT_PAGE, DEFAULT_LIMIT, DEFAULT_FILTERS).subscribe(result => {
            expect(result.page).toBe(DEFAULT_PAGE);
            expect(result.limit).toBe(DEFAULT_LIMIT);
            expect(result.data.length).toBe(1);
            expect(result.data[0].id).toBe('m1');
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_LIMIT));
        expect(req.request.params.get('dateFrom')).toBe('2026-03-01');
        expect(req.request.params.get('dateTo')).toBe('2026-03-31');
        req.flush(MOCK_PAGE_DTO);
    });

    it('should map meal response to meal on query', () => {
        service.query(DEFAULT_PAGE, DEFAULT_LIMIT, DEFAULT_FILTERS).subscribe(result => {
            const meal = result.data[0];
            expect(meal.totalCalories).toBe(TOTAL_CALORIES);
            expect(meal.totalAlcohol).toBe(0);
            expect(meal.isNutritionAutoCalculated).toBe(true);
            expect(meal.preMealSatietyLevel).toBe(PRE_MEAL_SATIETY_LEVEL);
            expect(meal.postMealSatietyLevel).toBe(POST_MEAL_SATIETY_LEVEL);
            expect(meal.items).toEqual([]);
            expect(meal.aiSessions).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        req.flush(MOCK_PAGE_DTO);
    });

    it('should rethrow query errors', () => {
        service.query(DEFAULT_PAGE, DEFAULT_LIMIT, DEFAULT_FILTERS).subscribe({
            next: () => {
                expect.fail('Expected query to fail');
            },
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('MealService reads', () => {
    it('should get meal by id', () => {
        service.getById('m1').subscribe(result => {
            expect(result).not.toBeNull();
            expect(result?.id).toBe('m1');
            expect(result?.date).toBe('2026-03-28');
            expect(result?.mealType).toBe('LUNCH');
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_MEAL_DTO);
    });

    it('should return null on getById error', () => {
        service.getById('nonexistent').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/nonexistent`);
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });

    it('should normalize API meal type casing', () => {
        service.getById('m1').subscribe(result => {
            expect(result?.mealType).toBe('LUNCH');
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        req.flush({ ...MOCK_MEAL_DTO, mealType: 'Lunch' });
    });
});

describe('MealService AI nutrition mapping', () => {
    it('should treat legacy AI-only nutrition matching AI totals as automatic', () => {
        service.getById('m1').subscribe(result => {
            expect(result?.isNutritionAutoCalculated).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        req.flush(createAiOnlyMeal(AI_TOTAL_CALORIES));
    });

    it('should keep manual mode when AI meal nutrition differs from AI totals', () => {
        service.getById('m1').subscribe(result => {
            expect(result?.isNutritionAutoCalculated).toBe(false);
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        req.flush(createAiOnlyMeal(MANUAL_TOTAL_CALORIES));
    });
});

describe('MealService create', () => {
    it('should create meal', () => {
        const createData = createMealManageDto('2026-03-28');

        service.create(createData).subscribe(result => {
            expect(result).not.toBeNull();
            expect(result.id).toBe('m1');
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(MOCK_MEAL_DTO);
    });

    it('should rethrow create errors', () => {
        const createData = createMealManageDto();

        service.create(createData).subscribe({
            next: () => {
                expect.fail('Expected create to fail');
            },
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('MealService update', () => {
    it('should update meal via PATCH', () => {
        const updateData: MealManageDto = {
            date: new Date('2026-03-28'),
            comment: 'Updated',
            items: [],
            isNutritionAutoCalculated: true,
        };

        service.update('m1', updateData).subscribe(result => {
            expect(result).not.toBeNull();
            expect(result.id).toBe('m1');
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(MOCK_MEAL_DTO);
    });

    it('should rethrow update errors', () => {
        const updateData: MealManageDto = {
            date: new Date(),
            comment: 'fail',
            items: [],
            isNutritionAutoCalculated: true,
        };

        service.update('m1', updateData).subscribe({
            next: () => {
                expect.fail('Expected update to fail');
            },
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('MealService delete', () => {
    it('should delete meal by id', () => {
        service.deleteById('m1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should rethrow delete errors', () => {
        service.deleteById('m1').subscribe({
            next: () => {
                expect.fail('Expected delete to fail');
            },
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/m1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

function createMealManageDto(date?: string): MealManageDto {
    return {
        date: date === undefined ? new Date() : new Date(date),
        mealType: 'lunch',
        items: [],
        isNutritionAutoCalculated: true,
    };
}

function createAiOnlyMeal(totalCalories: number): MealResponseDto {
    return {
        ...MOCK_MEAL_DTO,
        items: [],
        isNutritionAutoCalculated: false,
        totalCalories,
        totalProteins: AI_TOTAL_PROTEINS,
        totalFats: AI_TOTAL_FATS,
        totalCarbs: AI_TOTAL_CARBS,
        totalFiber: AI_TOTAL_FIBER,
        totalAlcohol: 0,
        manualCalories: totalCalories,
        manualProteins: AI_TOTAL_PROTEINS,
        manualFats: AI_TOTAL_FATS,
        manualCarbs: AI_TOTAL_CARBS,
        manualFiber: AI_TOTAL_FIBER,
        manualAlcohol: 0,
        aiSessions: [
            {
                id: 's1',
                mealId: 'm1',
                recognizedAtUtc: '2026-05-03T00:29:00Z',
                items: [
                    {
                        id: 'ai1',
                        sessionId: 's1',
                        nameEn: 'Banana porridge',
                        amount: 1,
                        unit: 'serving',
                        calories: AI_TOTAL_CALORIES,
                        proteins: AI_TOTAL_PROTEINS,
                        fats: AI_TOTAL_FATS,
                        carbs: AI_TOTAL_CARBS,
                        fiber: AI_TOTAL_FIBER,
                        alcohol: 0,
                    },
                ],
            },
        ],
    };
}

describe('MealService daily overview', () => {
    it('sends local timezone and favorites options and preserves server day totals', () => {
        const summaries = [{ date: '2026-03-28', totalCalories: 2500, mealCount: 8 }];
        service.queryOverview(1, DEFAULT_LIMIT, DEFAULT_FILTERS, { limit: 3, include: false }).subscribe(result => {
            expect(result.daySummaries).toEqual(summaries);
            expect(result.allMeals.data[0].id).toBe('m1');
        });
        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/overview`);
        expect(req.request.params.get('timeZoneId')).toBe(new Intl.DateTimeFormat().resolvedOptions().timeZone);
        expect(req.request.params.get('timeZoneOffsetMinutes')).toBe(String(-new Date().getTimezoneOffset()));
        expect(req.request.params.get('includeFavorites')).toBe('false');
        expect(req.request.params.get('favoriteLimit')).toBe('3');
        req.flush({ allMeals: MOCK_PAGE_DTO, favoriteItems: [], favoriteTotalCount: 0, daySummaries: summaries });
    });
    it('requests favorites by default and tolerates older responses without totals', () => {
        service.queryOverview(1, DEFAULT_LIMIT, {}).subscribe(result => {
            expect(result.daySummaries).toEqual([]);
        });
        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/overview`);
        expect(req.request.params.get('includeFavorites')).toBe('true');
        req.flush({ allMeals: MOCK_PAGE_DTO, favoriteItems: [], favoriteTotalCount: 0 });
    });
});

describe('MealService real item snapshots', () => {
    it.each(['g', 'G', 'ml', 'ML', 'pcs', 'PCS', '', 'unknown', null, undefined])('normalizes product unit %s', unit => {
        const item: MealItemResponseDto = { id: 'i', mealId: 'm1', amount: 2, productId: 'p', productBaseUnit: unit };
        service.getById('m1').subscribe(meal => {
            expect(meal?.items[0].sourceType).toBe(MealSourceType.Product);
            expect(meal?.items[0].product).toMatchObject({
                id: 'p',
                baseUnit: ['ml', 'ML'].includes(unit ?? '') ? 'ML' : ['pcs', 'PCS'].includes(unit ?? '') ? 'PCS' : 'G',
                baseAmount: 1,
                caloriesPerBase: 0,
            });
            expect(meal?.items[0].recipe).toBeNull();
        });
        httpMock.expectOne(`${BASE_URL}/m1`).flush({ ...MOCK_MEAL_DTO, items: [item] });
    });
});

describe('MealService populated snapshots', () => {
    it('preserves product and recipe snapshot nutrition, images and AI provenance', () => {
        const product: MealItemResponseDto = {
            id: 'p-item',
            mealId: 'm1',
            amount: 2,
            productId: 'p',
            productName: 'Milk',
            productImageUrl: '/milk',
            productBaseUnit: 'ml',
            productBaseAmount: 100,
            productCaloriesPerBase: 50,
            productProteinsPerBase: 3,
            productFatsPerBase: 2,
            productCarbsPerBase: 4,
            productFiberPerBase: 1,
            productAlcoholPerBase: 0,
            sourceAiItemId: 'ai-item',
            origin: 'Ai',
        };
        const recipe: MealItemResponseDto = {
            id: 'r-item',
            mealId: 'm1',
            amount: 1,
            recipeId: 'r',
            recipeName: 'Soup',
            recipeImageUrl: '/soup',
            recipeServings: 2,
            recipeTotalCalories: 400,
            recipeTotalProteins: 20,
            recipeTotalFats: 10,
            recipeTotalCarbs: 30,
            recipeTotalFiber: 8,
            recipeTotalAlcohol: 0,
        };
        service.getById('m1').subscribe(meal => {
            expect(meal?.items[0]).toMatchObject({
                amount: 2,
                sourceAiItemId: 'ai-item',
                origin: 'Ai',
                product: {
                    name: 'Milk',
                    imageUrl: '/milk',
                    baseAmount: 100,
                    caloriesPerBase: 50,
                    proteinsPerBase: 3,
                    fatsPerBase: 2,
                    carbsPerBase: 4,
                    fiberPerBase: 1,
                    alcoholPerBase: 0,
                },
            });
            expect(meal?.items[1]).toMatchObject({
                sourceType: MealSourceType.Recipe,
                product: null,
                recipe: {
                    id: 'r',
                    name: 'Soup',
                    imageUrl: '/soup',
                    servings: 2,
                    totalCalories: 400,
                    totalProteins: 20,
                    totalFats: 10,
                    totalCarbs: 30,
                    totalFiber: 8,
                    totalAlcohol: 0,
                },
            });
        });
        httpMock.expectOne(`${BASE_URL}/m1`).flush({ ...MOCK_MEAL_DTO, items: [product, recipe] });
    });
});

describe('MealService missing snapshot fields', () => {
    it('defaults optional recipe snapshot fields and keeps absent sources null', () => {
        service.getById('m1').subscribe(meal => {
            expect(meal?.items[0].recipe).toMatchObject({
                id: 'r',
                name: '',
                imageUrl: null,
                servings: 1,
                totalCalories: 0,
                totalProteins: 0,
                totalFats: 0,
                totalCarbs: 0,
                totalFiber: 0,
                totalAlcohol: 0,
            });
            expect(meal?.items[1]).toMatchObject({ product: null, recipe: null, sourceAiItemId: null, origin: null });
        });
        httpMock.expectOne(`${BASE_URL}/m1`).flush({
            ...MOCK_MEAL_DTO,
            items: [
                { id: 'r', mealId: 'm1', amount: 1, recipeId: 'r' },
                { id: 'missing', mealId: 'm1', amount: 1, productId: '', recipeId: '' },
            ],
        });
    });
});

describe('MealService repeat and overview transport', () => {
    it.each(['Dinner', undefined])('repeats at the supplied instant with type %s and maps the result', type => {
        const date = '2026-01-01T21:00:00.000Z';
        service.repeat('original', date, type).subscribe(meal => {
            expect(meal.id).toBe('m1');
        });
        const request = httpMock.expectOne(`${BASE_URL}/original/repeat`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({ targetDate: date, mealType: type });
        request.flush(MOCK_MEAL_DTO);
    });
    it.each(['repeat', 'overview'] as const)('propagates %s failures', operation => {
        const request: Observable<unknown> =
            operation === 'repeat' ? service.repeat('m1', '2026-01-01T00:00:00Z') : service.queryOverview(1, DEFAULT_LIMIT, {});
        let failed = false;
        request.subscribe({
            next: () => {
                throw new Error('must not succeed');
            },
            error: () => {
                failed = true;
            },
        });
        httpMock
            .expectOne(req => req.url.endsWith(operation === 'repeat' ? '/m1/repeat' : '/overview'))
            .flush({}, { status: 500, statusText: 'failure' });
        expect(failed).toBe(true);
    });
    it('sends zero and false filters without dropping them', () => {
        service
            .query(1, DEFAULT_LIMIT, { caloriesFrom: 0, caloriesTo: 0, hasImage: false, hasAiSession: false, mealTypes: 'Dinner,Snack' })
            .subscribe();
        const request = httpMock.expectOne(req => req.url === `${BASE_URL}/`);
        expect(request.request.params.get('caloriesFrom')).toBe('0');
        expect(request.request.params.get('caloriesTo')).toBe('0');
        expect(request.request.params.get('hasImage')).toBe('false');
        expect(request.request.params.get('hasAiSession')).toBe('false');
        expect(request.request.params.get('mealTypes')).toBe('Dinner,Snack');
        request.flush(MOCK_PAGE_DTO);
    });
});
