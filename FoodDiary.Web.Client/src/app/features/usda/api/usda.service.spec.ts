import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { USDA_NUTRIENT_IDS } from '../../../shared/lib/usda-nutrient.constants';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { usdaFoodId } from '../../../shared/models/semantics/usda-food-id';
import type { DailyMicronutrientSummary, UsdaFood, UsdaFoodDetail } from '../../../shared/models/usda.data';
import { UsdaService } from './usda.service';
import { USDA_SEARCH_LIMIT } from './usda-api.tokens';

const BASE_URL = environment.apiUrls.usda;
const DEFAULT_SEARCH_LIMIT = 20;
const CUSTOM_SEARCH_LIMIT = 5;
const FDC_ID = 17_000;
const PRODUCT_ID = 'product-1';
const ENERGY_AMOUNT = 203;
const PROTEIN_AMOUNT = 1.35;
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
        providers: [
            UsdaService,
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: USDA_SEARCH_LIMIT, useValue: DEFAULT_SEARCH_LIMIT },
        ],
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
});
