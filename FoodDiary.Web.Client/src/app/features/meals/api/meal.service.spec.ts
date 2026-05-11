import { type HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { PageOf } from '../../../shared/models/page-of.data';
import type { ConsumptionManageDto, ConsumptionResponseDto, MealFilters } from '../models/meal.data';
import { MealService } from './meal.service';

const DEFAULT_PAGE = 1;
const DEFAULT_LIMIT = 10;
const SERVER_ERROR_STATUS = 500;
const TOTAL_CALORIES = 500;
const PRE_MEAL_SATIETY_LEVEL = 3;
const POST_MEAL_SATIETY_LEVEL = 4;
const AI_TOTAL_CALORIES = 319.2;
const AI_TOTAL_PROTEINS = 9.9;
const AI_TOTAL_FATS = 4.8;
const AI_TOTAL_CARBS = 67.09;
const AI_TOTAL_FIBER = 13.79;
const MANUAL_TOTAL_CALORIES = 350;

describe('MealService', () => {
    let service: MealService;
    let httpMock: HttpTestingController;
    const baseUrl = 'http://localhost:5300/api/v1/consumptions';

    const mockConsumptionDto: ConsumptionResponseDto = {
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

    const mockPageDto: PageOf<ConsumptionResponseDto> = {
        data: [mockConsumptionDto],
        page: DEFAULT_PAGE,
        limit: DEFAULT_LIMIT,
        totalPages: 1,
        totalItems: 1,
    };

    const defaultFilters: MealFilters = {
        dateFrom: '2026-03-01',
        dateTo: '2026-03-31',
    };

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

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should query meals with pagination and filters', () => {
        service.query(DEFAULT_PAGE, DEFAULT_LIMIT, defaultFilters).subscribe(result => {
            expect(result.page).toBe(DEFAULT_PAGE);
            expect(result.limit).toBe(DEFAULT_LIMIT);
            expect(result.data.length).toBe(1);
            expect(result.data[0].id).toBe('m1');
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_LIMIT));
        expect(req.request.params.get('dateFrom')).toBe('2026-03-01');
        expect(req.request.params.get('dateTo')).toBe('2026-03-31');
        req.flush(mockPageDto);
    });

    it('should map consumption response to meal on query', () => {
        service.query(DEFAULT_PAGE, DEFAULT_LIMIT, defaultFilters).subscribe(result => {
            const meal = result.data[0];
            expect(meal.totalCalories).toBe(TOTAL_CALORIES);
            expect(meal.totalAlcohol).toBe(0);
            expect(meal.isNutritionAutoCalculated).toBe(true);
            expect(meal.preMealSatietyLevel).toBe(PRE_MEAL_SATIETY_LEVEL);
            expect(meal.postMealSatietyLevel).toBe(POST_MEAL_SATIETY_LEVEL);
            expect(meal.items).toEqual([]);
            expect(meal.aiSessions).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        req.flush(mockPageDto);
    });

    it('should rethrow query errors', () => {
        service.query(DEFAULT_PAGE, DEFAULT_LIMIT, defaultFilters).subscribe({
            next: () => {
                expect.fail('Expected query to fail');
            },
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(SERVER_ERROR_STATUS);
            },
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        req.flush('Server Error', { status: SERVER_ERROR_STATUS, statusText: 'Internal Server Error' });
    });

    it('should get meal by id', () => {
        service.getById('m1').subscribe(result => {
            expect(result).not.toBeNull();
            expect(result?.id).toBe('m1');
            expect(result?.date).toBe('2026-03-28');
            expect(result?.mealType).toBe('LUNCH');
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        expect(req.request.method).toBe('GET');
        req.flush(mockConsumptionDto);
    });

    it('should return null on getById error', () => {
        service.getById('nonexistent').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/nonexistent`);
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });

    it('should create meal', () => {
        const createData: ConsumptionManageDto = {
            date: new Date('2026-03-28'),
            mealType: 'lunch',
            items: [],
            isNutritionAutoCalculated: true,
        };

        service.create(createData).subscribe(result => {
            expect(result).not.toBeNull();
            expect(result?.id).toBe('m1');
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(mockConsumptionDto);
    });

    it('should normalize API meal type casing', () => {
        service.getById('m1').subscribe(result => {
            expect(result?.mealType).toBe('LUNCH');
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        req.flush({ ...mockConsumptionDto, mealType: 'Lunch' });
    });

    it('should treat legacy AI-only nutrition matching AI totals as automatic', () => {
        service.getById('m1').subscribe(result => {
            expect(result?.isNutritionAutoCalculated).toBe(true);
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        req.flush({
            ...mockConsumptionDto,
            items: [],
            isNutritionAutoCalculated: false,
            totalCalories: AI_TOTAL_CALORIES,
            totalProteins: AI_TOTAL_PROTEINS,
            totalFats: AI_TOTAL_FATS,
            totalCarbs: AI_TOTAL_CARBS,
            totalFiber: AI_TOTAL_FIBER,
            totalAlcohol: 0,
            manualCalories: AI_TOTAL_CALORIES,
            manualProteins: AI_TOTAL_PROTEINS,
            manualFats: AI_TOTAL_FATS,
            manualCarbs: AI_TOTAL_CARBS,
            manualFiber: AI_TOTAL_FIBER,
            manualAlcohol: 0,
            aiSessions: [
                {
                    id: 's1',
                    consumptionId: 'm1',
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
        });
    });

    it('should keep manual mode when AI meal nutrition differs from AI totals', () => {
        service.getById('m1').subscribe(result => {
            expect(result?.isNutritionAutoCalculated).toBe(false);
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        req.flush({
            ...mockConsumptionDto,
            items: [],
            isNutritionAutoCalculated: false,
            totalCalories: MANUAL_TOTAL_CALORIES,
            manualCalories: MANUAL_TOTAL_CALORIES,
            manualProteins: AI_TOTAL_PROTEINS,
            manualFats: AI_TOTAL_FATS,
            manualCarbs: AI_TOTAL_CARBS,
            manualFiber: AI_TOTAL_FIBER,
            manualAlcohol: 0,
            aiSessions: [
                {
                    id: 's1',
                    consumptionId: 'm1',
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
        });
    });

    it('should return null on create error', () => {
        const createData: ConsumptionManageDto = {
            date: new Date(),
            mealType: 'lunch',
            items: [],
            isNutritionAutoCalculated: true,
        };

        service.create(createData).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server Error', { status: SERVER_ERROR_STATUS, statusText: 'Internal Server Error' });
    });

    it('should update meal via PATCH', () => {
        const updateData: ConsumptionManageDto = {
            date: new Date('2026-03-28'),
            comment: 'Updated',
            items: [],
            isNutritionAutoCalculated: true,
        };

        service.update('m1', updateData).subscribe(result => {
            expect(result).not.toBeNull();
            expect(result?.id).toBe('m1');
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(mockConsumptionDto);
    });

    it('should return null on update error', () => {
        const updateData: ConsumptionManageDto = {
            date: new Date(),
            comment: 'fail',
            items: [],
            isNutritionAutoCalculated: true,
        };

        service.update('m1', updateData).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        req.flush('Server Error', { status: SERVER_ERROR_STATUS, statusText: 'Internal Server Error' });
    });

    it('should delete meal by id', () => {
        service.deleteById('m1').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should rethrow delete errors', () => {
        service.deleteById('m1').subscribe({
            next: () => {
                expect.fail('Expected delete to fail');
            },
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(SERVER_ERROR_STATUS);
            },
        });

        const req = httpMock.expectOne(`${baseUrl}/m1`);
        req.flush('Server Error', { status: SERVER_ERROR_STATUS, statusText: 'Internal Server Error' });
    });
});
