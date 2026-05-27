import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { MealService } from '../../api/meal.service';
import type { Meal } from '../../models/meal.data';
import { AiMealCreateService } from './ai-meal-create.service';

const RECOGNIZED_AT_UTC = '2026-05-02T19:00:00.000Z';
const RESULT_DATE = '2026-05-02';
const RESULT_TIME = '23:50';

describe('AiMealCreateService', () => {
    it('maps AI input result and creates a meal through the API', () => {
        const { service, mealService } = setupService();

        service
            .createFromAiResult({
                source: 'Text',
                mealType: null,
                date: RESULT_DATE,
                time: RESULT_TIME,
                recognizedAtUtc: RECOGNIZED_AT_UTC,
                items: [],
            })
            .subscribe();

        expect(mealService.create).toHaveBeenCalledWith(
            expect.objectContaining({
                mealType: 'SNACK',
                isNutritionAutoCalculated: true,
                aiSessions: [expect.objectContaining({ source: 'Text', recognizedAtUtc: RECOGNIZED_AT_UTC })],
            }),
        );
    });
});

function setupService(): {
    service: AiMealCreateService;
    mealService: { create: ReturnType<typeof vi.fn> };
} {
    const mealService = {
        create: vi.fn(() => of(createMeal())),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [AiMealCreateService, { provide: MealService, useValue: mealService }],
    });

    return {
        service: TestBed.inject(AiMealCreateService),
        mealService,
    };
}

function createMeal(): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-02T23:50:00.000Z',
        mealType: 'SNACK',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: 0,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        items: [],
        aiSessions: [],
    };
}
