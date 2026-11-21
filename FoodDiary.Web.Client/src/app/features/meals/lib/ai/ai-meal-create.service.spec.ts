import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import type { Meal } from '../../../../shared/models/meal.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { MealService } from '../../api/meal.service';
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

    it('uses the current local time for a legacy recognition without editable date or time', () => {
        const { service, mealService } = setupService();
        const now = new Date('2026-05-03T00:20:15.025');
        vi.useFakeTimers({ toFake: ['Date'] });
        onTestFinished(() => {
            vi.useRealTimers();
        });
        vi.setSystemTime(now);
        const notes = 'A banana after the evening shift';
        const items = [
            {
                nameEn: 'Banana',
                amount: 120,
                unit: 'g',
                calories: 107,
                proteins: 1,
                fats: 0,
                carbs: 27,
                fiber: 3,
                alcohol: 0,
            },
        ];

        service.createFromAiResult({ source: 'Voice', recognizedAtUtc: RECOGNIZED_AT_UTC, notes, items }).subscribe();

        expect(mealService.create).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                date: now,
                mealType: 'SNACK',
                isNutritionAutoCalculated: true,
                aiSessions: [
                    expect.objectContaining({
                        source: 'Voice',
                        status: 'Reviewed',
                        recognizedAtUtc: RECOGNIZED_AT_UTC,
                        notes,
                        items,
                    }),
                ],
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
        id: entityId<'meal'>('meal-1'),
        date: utcInstant('2026-05-02T23:50:00.000Z'),
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
