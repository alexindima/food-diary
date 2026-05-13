import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import type { FoodNutritionRequest, FoodVisionRequest } from '../models/ai.data';
import { AiFoodService } from './ai-food.service';

const BASE_URL = environment.apiUrls.ai;
const VISION_ITEM_COUNT = 2;
const CHICKEN_AMOUNT_GRAMS = 200;
const CHICKEN_CALORIES = 330;
const CHICKEN_PROTEIN = 62;
const CHICKEN_FAT = 7.2;
const INPUT_LIMIT = 10000;
const OUTPUT_LIMIT = 5000;
const INPUT_USED = 2500;
const OUTPUT_USED = 1200;

let service: AiFoodService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [AiFoodService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AiFoodService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('AiFoodService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});

describe('AiFoodService analysis', () => {
    it('should analyze food image (POST /api/v1/ai/food/vision)', () => {
        const request: FoodVisionRequest = {
            imageAssetId: 'asset-123',
            description: 'A bowl of salad',
        };
        const response = {
            items: [
                { nameEn: 'Lettuce', amount: 100, unit: 'g', confidence: 0.95 },
                { nameEn: 'Tomato', amount: 50, unit: 'g', confidence: 0.9 },
            ],
            notes: `Detected ${VISION_ITEM_COUNT} items`,
        };

        service.analyzeFoodImage(request).subscribe(result => {
            expect(result.items.length).toBe(VISION_ITEM_COUNT);
            expect(result.items[0].nameEn).toBe('Lettuce');
            expect(result.notes).toBe(`Detected ${VISION_ITEM_COUNT} items`);
        });

        const req = httpMock.expectOne(`${BASE_URL}/food/vision`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(response);
    });

    it('should calculate nutrition (POST /api/v1/ai/food/nutrition)', () => {
        const request: FoodNutritionRequest = {
            items: [{ nameEn: 'Chicken breast', amount: CHICKEN_AMOUNT_GRAMS, unit: 'g', confidence: 0.95 }],
        };
        const response = createNutritionResponse();

        service.calculateNutrition(request).subscribe(result => {
            expect(result.calories).toBe(CHICKEN_CALORIES);
            expect(result.protein).toBe(CHICKEN_PROTEIN);
            expect(result.items.length).toBe(1);
        });

        const req = httpMock.expectOne(`${BASE_URL}/food/nutrition`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(response);
    });
});

describe('AiFoodService usage', () => {
    it('should get usage summary (GET /api/v1/ai/usage/me)', () => {
        const response = {
            inputLimit: INPUT_LIMIT,
            outputLimit: OUTPUT_LIMIT,
            inputUsed: INPUT_USED,
            outputUsed: OUTPUT_USED,
            resetAtUtc: '2026-04-01T00:00:00Z',
        };

        service.getUsageSummary().subscribe(result => {
            expect(result?.inputLimit).toBe(INPUT_LIMIT);
            expect(result?.inputUsed).toBe(INPUT_USED);
            expect(result?.resetAtUtc).toBe('2026-04-01T00:00:00Z');
        });

        const req = httpMock.expectOne(`${BASE_URL}/usage/me`);
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('should return null when getUsageSummary fails', () => {
        service.getUsageSummary().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/usage/me`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

function createNutritionResponse(): {
    alcohol: number;
    calories: number;
    carbs: number;
    fat: number;
    fiber: number;
    items: Array<{
        alcohol: number;
        amount: number;
        calories: number;
        carbs: number;
        fat: number;
        fiber: number;
        name: string;
        protein: number;
        unit: string;
    }>;
    notes: null;
    protein: number;
} {
    return {
        calories: CHICKEN_CALORIES,
        protein: CHICKEN_PROTEIN,
        fat: CHICKEN_FAT,
        carbs: 0,
        fiber: 0,
        alcohol: 0,
        items: [
            {
                name: 'Chicken breast',
                amount: CHICKEN_AMOUNT_GRAMS,
                unit: 'g',
                calories: CHICKEN_CALORIES,
                protein: CHICKEN_PROTEIN,
                fat: CHICKEN_FAT,
                carbs: 0,
                fiber: 0,
                alcohol: 0,
            },
        ],
        notes: null,
    };
}
