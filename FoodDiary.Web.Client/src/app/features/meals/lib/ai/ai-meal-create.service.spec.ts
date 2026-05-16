import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

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
});

function setupService(): {
    service: AiMealCreateService;
    mealService: { create: ReturnType<typeof vi.fn> };
} {
    const mealService = {
        create: vi.fn(() => of(null)),
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
