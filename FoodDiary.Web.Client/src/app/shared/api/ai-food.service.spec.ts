import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AiFoodService } from './ai-food.service';
import { environment } from '../../../environments/environment';
import { FoodNutritionRequest, FoodVisionRequest } from '../models/ai.data';

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
            notes: 'Detected 2 items',
        };

        service.analyzeFoodImage(request).subscribe(response => {
            expect(response.items.length).toBe(2);
            expect(response.items[0].nameEn).toBe('Lettuce');
            expect(response.notes).toBe('Detected 2 items');
        });

        const req = httpMock.expectOne(`${baseUrl}/food/vision`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });

    it('should calculate nutrition (POST /api/v1/ai/food/nutrition)', () => {
        const request: FoodNutritionRequest = {
            items: [{ nameEn: 'Chicken breast', amount: 200, unit: 'g', confidence: 0.95 }],
        };

        const mockResponse = {
            calories: 330,
            protein: 62,
            fat: 7.2,
            carbs: 0,
            fiber: 0,
            alcohol: 0,
            items: [
                {
                    name: 'Chicken breast',
                    amount: 200,
                    unit: 'g',
                    calories: 330,
                    protein: 62,
                    fat: 7.2,
                    carbs: 0,
                    fiber: 0,
                    alcohol: 0,
                },
            ],
            notes: null,
        };

        service.calculateNutrition(request).subscribe(response => {
            expect(response.calories).toBe(330);
            expect(response.protein).toBe(62);
            expect(response.items.length).toBe(1);
        });

        const req = httpMock.expectOne(`${baseUrl}/food/nutrition`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });

    it('should get usage summary (GET /api/v1/ai/usage/me)', () => {
        const mockResponse = {
            inputLimit: 10000,
            outputLimit: 5000,
            inputUsed: 2500,
            outputUsed: 1200,
            resetAtUtc: '2026-04-01T00:00:00Z',
        };

        service.getUsageSummary().subscribe(response => {
            expect(response!.inputLimit).toBe(10000);
            expect(response!.inputUsed).toBe(2500);
            expect(response!.resetAtUtc).toBe('2026-04-01T00:00:00Z');
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
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
});
