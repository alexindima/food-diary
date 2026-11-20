import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { DailyMicronutrientSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/daily-micronutrient-summary-http-response';
import type { HealthAreaScoresHttpResponse } from '../../../shared/api/sdk/generated/model/health-area-scores-http-response';
import type { UsdaFoodDetailHttpResponse } from '../../../shared/api/sdk/generated/model/usda-food-detail-http-response';
import { USDA_NUTRIENT_IDS } from '../../../shared/lib/usda-nutrient.constants';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { usdaFoodId } from '../../../shared/models/semantics/usda-food-id';
import type { DailyMicronutrientSummary, UsdaFood, UsdaFoodDetail } from '../../../shared/models/usda.data';
import { UsdaService } from './usda.service';

const BASE_URL = environment.apiUrls.usda;
const DEFAULT_SEARCH_LIMIT = 20;
const CUSTOM_SEARCH_LIMIT = 5;
const FDC_ID = 17_000;
const PRODUCT_ID = '12345678-1234-1234-1234-123456789abc';
const PRODUCT_NOT_ACCESSIBLE = { error: 'Product.NotAccessible' } satisfies ApiErrorHttpResponse;
const ENERGY_AMOUNT = 203;
const PROTEIN_AMOUNT = 1.35;
const VITAMIN_C_AMOUNT_MG = 8.4;
const VITAMIN_C_DAILY_VALUE_MG = 90;
const DAILY_VALUE_PERCENT = 9.33;
const CUP_WEIGHT_GRAMS = 109;
const SLICE_WEIGHT_GRAMS = 28;
const HEALTH_SCORES = {
    heart: { score: 82, grade: 'excellent' },
    bone: { score: 48, grade: 'fair' },
    immune: { score: 68, grade: 'good' },
    energy: { score: 23, grade: 'low' },
    antioxidant: { score: 0, grade: 'unknown' },
} satisfies HealthAreaScoresHttpResponse;
const FOOD: UsdaFood = {
    fdcId: usdaFoodId(FDC_ID),
    description: 'Apple',
    foodCategory: 'Fruit',
};
const FOOD_DETAIL: UsdaFoodDetail = {
    fdcId: usdaFoodId(FDC_ID),
    description: 'Apple',
    foodCategory: 'Fruit',
    nutrients: [],
    portions: [],
    healthScores: null,
};
const SUMMARY: DailyMicronutrientSummary = {
    date: '2026-05-15',
    linkedProductCount: 1,
    totalProductCount: 2,
    nutrients: [],
    healthScores: null,
};

let service: UsdaService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [UsdaService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(UsdaService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('UsdaService foods', () => {
    it('searches foods with default limit token', () => {
        service.searchFoods('apple').subscribe(foods => {
            expect(foods).toEqual([FOOD]);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/foods`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('search')).toBe('apple');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_SEARCH_LIMIT));
        req.flush([FOOD]);
    });

    it('searches foods with explicit limit', () => {
        service.searchFoods('apple', CUSTOM_SEARCH_LIMIT).subscribe();

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/foods`);
        expect(req.request.params.get('limit')).toBe(String(CUSTOM_SEARCH_LIMIT));
        req.flush([]);
    });

    it('returns empty foods on search error', () => {
        service.searchFoods('apple').subscribe(foods => {
            expect(foods).toEqual([]);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/foods`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('gets food detail', () => {
        service.getFoodDetail(usdaFoodId(FDC_ID)).subscribe(detail => {
            expect(detail).toEqual(FOOD_DETAIL);
        });

        const req = httpMock.expectOne(`${BASE_URL}/foods/${FDC_ID}`);
        expect(req.request.method).toBe('GET');
        req.flush(FOOD_DETAIL);
    });

    it('maps the API nutrient amount spelling to finite food-detail values', () => {
        const wireNutrients = [
            {
                nutrientId: USDA_NUTRIENT_IDS.energy,
                name: 'Energy',
                unit: 'kcal',
                amountPer100G: ENERGY_AMOUNT,
                dailyValue: null,
                percentDailyValue: null,
            },
            {
                nutrientId: USDA_NUTRIENT_IDS.protein,
                name: 'Protein',
                unit: 'g',
                amountPer100G: PROTEIN_AMOUNT,
                dailyValue: null,
                percentDailyValue: null,
            },
        ];

        service.getFoodDetail(usdaFoodId(FDC_ID)).subscribe(detail => {
            expect(detail).toEqual({
                ...FOOD_DETAIL,
                nutrients: [
                    { ...wireNutrients[0], amountPer100G: undefined, amountPer100g: ENERGY_AMOUNT },
                    { ...wireNutrients[1], amountPer100G: undefined, amountPer100g: PROTEIN_AMOUNT },
                ],
            });
            expect(detail.nutrients.every(nutrient => Number.isFinite(nutrient.amountPer100g))).toBe(true);
            expect(detail.nutrients.every(nutrient => !('amountPer100G' in nutrient))).toBe(true);
        });

        httpMock.expectOne(`${BASE_URL}/foods/${FDC_ID}`).flush({ ...FOOD_DETAIL, nutrients: wireNutrients });
    });
});

describe('UsdaService product link', () => {
    it('links and unlinks product', () => {
        service.linkProduct(entityId<'product'>(PRODUCT_ID), usdaFoodId(FDC_ID)).subscribe();

        const linkReq = httpMock.expectOne(`${BASE_URL}/products/${PRODUCT_ID}/link`);
        expect(linkReq.request.method).toBe('PUT');
        expect(linkReq.request.body).toEqual({ fdcId: usdaFoodId(FDC_ID) });
        linkReq.flush(null);

        service.unlinkProduct(entityId<'product'>(PRODUCT_ID)).subscribe();

        const unlinkReq = httpMock.expectOne(`${BASE_URL}/products/${PRODUCT_ID}/link`);
        expect(unlinkReq.request.method).toBe('DELETE');
        unlinkReq.flush(null);
    });

    it('propagates the API error when linking a product the user cannot access', async () => {
        const pending = firstValueFrom(service.linkProduct(entityId<'product'>(PRODUCT_ID), usdaFoodId(FDC_ID)));
        httpMock.expectOne({ method: 'PUT', url: `${BASE_URL}/products/${PRODUCT_ID}/link` }).flush(PRODUCT_NOT_ACCESSIBLE, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: PRODUCT_NOT_ACCESSIBLE });
    });

    it('propagates an inaccessible product when unlinking and allows another attempt', async () => {
        const product = entityId<'product'>(PRODUCT_ID);
        const pending = firstValueFrom(service.unlinkProduct(product));
        httpMock.expectOne({ method: 'DELETE', url: `${BASE_URL}/products/${PRODUCT_ID}/link` }).flush(PRODUCT_NOT_ACCESSIBLE, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });
        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: PRODUCT_NOT_ACCESSIBLE });

        const retry = firstValueFrom(service.unlinkProduct(product));
        httpMock.expectOne({ method: 'DELETE', url: `${BASE_URL}/products/${PRODUCT_ID}/link` }).flush(null, {
            status: HttpStatusCode.NoContent,
            statusText: 'No Content',
        });
        await expect(retry).resolves.toBeUndefined();
    });
});

describe('USDA catalog response semantics', () => {
    it('preserves a search result whose source omitted its category', async () => {
        const pending = firstValueFrom(service.searchFoods('apple'));
        httpMock.expectOne(request => request.url === `${BASE_URL}/foods`).flush([{ fdcId: FDC_ID, description: FOOD.description }]);

        await expect(pending).resolves.toEqual([{ ...FOOD, foodCategory: null }]);
    });

    it('keeps nutrient amounts per 100 g distinct from portion weights and preserves optional metadata', async () => {
        const vitamin = {
            nutrientId: USDA_NUTRIENT_IDS.vitaminC,
            name: 'Vitamin C',
            unit: 'mg',
        };
        const cup = { id: 1, amount: 1, gramWeight: CUP_WEIGHT_GRAMS, measureUnitName: 'cup', modifier: 'chopped' };
        const slice = { id: 2, amount: 1, gramWeight: SLICE_WEIGHT_GRAMS, measureUnitName: 'slice', portionDescription: 'thin slice' };
        const payload = {
            fdcId: FDC_ID,
            description: FOOD.description,
            nutrients: [
                {
                    ...vitamin,
                    amountPer100G: VITAMIN_C_AMOUNT_MG,
                    dailyValue: VITAMIN_C_DAILY_VALUE_MG,
                    percentDailyValue: DAILY_VALUE_PERCENT,
                },
            ],
            portions: [cup, slice],
            healthScores: HEALTH_SCORES,
        } satisfies UsdaFoodDetailHttpResponse;
        const pending = firstValueFrom(service.getFoodDetail(usdaFoodId(FDC_ID)));
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/foods/${FDC_ID}` }).flush(payload);

        await expect(pending).resolves.toEqual({
            fdcId: usdaFoodId(FDC_ID),
            description: FOOD.description,
            foodCategory: null,
            healthScores: HEALTH_SCORES,
            nutrients: [
                {
                    ...vitamin,
                    amountPer100g: VITAMIN_C_AMOUNT_MG,
                    dailyValue: VITAMIN_C_DAILY_VALUE_MG,
                    percentDailyValue: DAILY_VALUE_PERCENT,
                },
            ],
            portions: [
                { ...cup, portionDescription: null },
                { ...slice, modifier: null },
            ],
        });
    });

    it('rejects a detailed response without its portion collection', async () => {
        const pending = firstValueFrom(service.getFoodDetail(usdaFoodId(FDC_ID)));
        const incomplete = { fdcId: FDC_ID, description: FOOD.description, nutrients: [] } satisfies UsdaFoodDetailHttpResponse;
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/foods/${FDC_ID}` }).flush(incomplete);

        await expect(pending).rejects.toThrow('portions');
    });
});

describe('UsdaService daily micronutrients', () => {
    it('gets daily micronutrients for date', () => {
        service.getDailyMicronutrients(SUMMARY.date).subscribe(summary => {
            expect(summary).toEqual(SUMMARY);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/daily-micronutrients`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('date')).toBe(SUMMARY.date);
        req.flush(SUMMARY);
    });

    it('returns empty daily micronutrient summary on error', () => {
        service.getDailyMicronutrients(SUMMARY.date).subscribe(summary => {
            expect(summary).toEqual({
                date: SUMMARY.date,
                linkedProductCount: 0,
                totalProductCount: 0,
                nutrients: [],
                healthScores: null,
            });
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/daily-micronutrients`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('preserves total intake, a recorded zero, and the distinction between missing and known daily values', async () => {
        const protein = { nutrientId: USDA_NUTRIENT_IDS.protein, name: 'Protein', unit: 'g', totalAmount: 0 };
        const vitamin = {
            nutrientId: USDA_NUTRIENT_IDS.vitaminC,
            name: 'Vitamin C',
            unit: 'mg',
            totalAmount: VITAMIN_C_AMOUNT_MG,
            dailyValue: VITAMIN_C_DAILY_VALUE_MG,
            percentDailyValue: DAILY_VALUE_PERCENT,
        };
        const payload = {
            date: SUMMARY.date,
            linkedProductCount: SUMMARY.linkedProductCount,
            totalProductCount: SUMMARY.totalProductCount,
            nutrients: [protein, vitamin],
        } satisfies DailyMicronutrientSummaryHttpResponse;
        const pending = firstValueFrom(service.getDailyMicronutrients(SUMMARY.date));
        const request = httpMock.expectOne(candidate => candidate.url === `${BASE_URL}/daily-micronutrients`);
        expect(request.request.params.get('date')).toBe(SUMMARY.date);
        request.flush(payload);

        await expect(pending).resolves.toEqual({
            ...SUMMARY,
            nutrients: [{ ...protein, dailyValue: null, percentDailyValue: null }, vitamin],
        });
    });
});
