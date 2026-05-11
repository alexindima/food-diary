import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import type { FoodNutritionRequest, FoodVisionRequest } from '../models/ai.data';
import { AiFoodService } from './ai-food.service';

const VISION_ITEM_COUNT = 2;
const CHICKEN_AMOUNT_GRAMS = 200;
const CHICKEN_CALORIES = 330;
const CHICKEN_PROTEIN = 62;
const CHICKEN_FAT = 7.2;
const INPUT_LIMIT = 10000;
const OUTPUT_LIMIT = 5000;
const INPUT_USED = 2500;
const OUTPUT_USED = 1200;
const HTTP_INTERNAL_SERVER_ERROR = 500;

describe('AiFoodService', () => {
    let service: AiFoodService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.ai;

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

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should analyze food image (POST /api/v1/ai/food/vision)', () => {
        const request: FoodVisionRequest = {
            imageAssetId: 'asset-123',
            description: 'A bowl of salad',
        };

        const mockResponse = {
            items: [
                { nameEn: 'Lettuce', amount: 100, unit: 'g', confidence: 0.95 },
                { nameEn: 'Tomato', amount: 50, unit: 'g', confidence: 0.9 },
            ],
            notes: `Detected ${VISION_ITEM_COUNT} items`,
        };

        service.analyzeFoodImage(request).subscribe(response => {
            expect(response.items.length).toBe(VISION_ITEM_COUNT);
            expect(response.items[0].nameEn).toBe('Lettuce');
            expect(response.notes).toBe(`Detected ${VISION_ITEM_COUNT} items`);
        });

        const req = httpMock.expectOne(`${baseUrl}/food/vision`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });

    it('should calculate nutrition (POST /api/v1/ai/food/nutrition)', () => {
        const request: FoodNutritionRequest = {
            items: [{ nameEn: 'Chicken breast', amount: CHICKEN_AMOUNT_GRAMS, unit: 'g', confidence: 0.95 }],
        };

        const mockResponse = {
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

        service.calculateNutrition(request).subscribe(response => {
            expect(response.calories).toBe(CHICKEN_CALORIES);
            expect(response.protein).toBe(CHICKEN_PROTEIN);
            expect(response.items.length).toBe(1);
        });

        const req = httpMock.expectOne(`${baseUrl}/food/nutrition`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });

    it('should get usage summary (GET /api/v1/ai/usage/me)', () => {
        const mockResponse = {
            inputLimit: INPUT_LIMIT,
            outputLimit: OUTPUT_LIMIT,
            inputUsed: INPUT_USED,
            outputUsed: OUTPUT_USED,
            resetAtUtc: '2026-04-01T00:00:00Z',
        };

        service.getUsageSummary().subscribe(response => {
            expect(response?.inputLimit).toBe(INPUT_LIMIT);
            expect(response?.inputUsed).toBe(INPUT_USED);
            expect(response?.resetAtUtc).toBe('2026-04-01T00:00:00Z');
        });

        const req = httpMock.expectOne(`${baseUrl}/usage/me`);
        expect(req.request.method).toBe('GET');
        req.flush(mockResponse);
    });

    it('should return null when getUsageSummary fails', () => {
        service.getUsageSummary().subscribe(response => {
            expect(response).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/usage/me`);
        req.flush('Server error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });
});
