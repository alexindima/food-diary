import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { NutritionDataInvalidationService } from '../../../../shared/state/nutrition-data-invalidation.service';
import type { Meal } from '../../models/meal.data';
import { AiMealCreateFacade } from './ai-meal-create.facade';
import { AiMealCreateService } from './ai-meal-create.service';

const AI_RESULT: AiInputBarResult = {
    source: 'Text',
    mealType: 'SNACK',
    date: '2026-05-17',
    time: '12:30',
    recognizedAtUtc: '2026-05-17T08:30:00.000Z',
    items: [
        {
            nameEn: 'apple',
            nameLocal: null,
            amount: 120,
            unit: 'g',
            calories: 52,
            proteins: 0,
            fats: 0,
            carbs: 14,
            fiber: 2,
            alcohol: 0,
        },
    ],
};

describe('AiMealCreateFacade', () => {
    it('sets saving state and increments clear token only after successful create', () => {
        const meal = createMeal();
        const { facade, aiMealCreateService, invalidation } = setupFacade(of(meal));

        let actual: Meal | null = null;
        facade.createFromAiResult(AI_RESULT).subscribe(result => {
            actual = result;
        });

        expect(actual).toBe(meal);
        expect(aiMealCreateService.createFromAiResult).toHaveBeenCalledWith(AI_RESULT);
        expect(facade.clearToken()).toBe(1);
        expect(facade.isSaving()).toBe(false);
        expect(facade.errorKey()).toBeNull();
        expect(invalidation.dashboardVersion()).toBe(1);
    });

    it('keeps clear token unchanged and shows a toast on create failure', () => {
        const { facade, toastService } = setupFacade(throwError(() => new Error('boom')));

        let actual: Meal | null | undefined;
        facade.createFromAiResult(AI_RESULT).subscribe(result => {
            actual = result;
        });

        expect(actual).toBeNull();
        expect(facade.clearToken()).toBe(0);
        expect(facade.errorKey()).toBe('AI_INPUT_BAR.CREATE_MEAL_ERROR');
        expect(toastService.error).toHaveBeenCalledWith('AI_INPUT_BAR.CREATE_MEAL_ERROR');
        expect(facade.isSaving()).toBe(false);
    });

    it('ignores duplicate create while a save is in flight', () => {
        const pending = new Subject<Meal>();
        const { facade, aiMealCreateService } = setupFacade(pending.asObservable());

        facade.createFromAiResult(AI_RESULT).subscribe();
        facade.createFromAiResult(AI_RESULT).subscribe();

        expect(aiMealCreateService.createFromAiResult).toHaveBeenCalledOnce();
        expect(facade.isSaving()).toBe(true);

        pending.next(createMeal());
        pending.complete();

        expect(facade.isSaving()).toBe(false);
    });
});

function setupFacade(response$: Observable<Meal>): {
    aiMealCreateService: { createFromAiResult: ReturnType<typeof vi.fn> };
    facade: AiMealCreateFacade;
    invalidation: NutritionDataInvalidationService;
    toastService: { error: ReturnType<typeof vi.fn> };
} {
    const aiMealCreateService = {
        createFromAiResult: vi.fn(() => response$),
    };
    const toastService = {
        error: vi.fn(),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [
            AiMealCreateFacade,
            { provide: AiMealCreateService, useValue: aiMealCreateService },
            { provide: FdUiToastService, useValue: toastService },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
        ],
    });

    return {
        aiMealCreateService,
        facade: TestBed.inject(AiMealCreateFacade),
        invalidation: TestBed.inject(NutritionDataInvalidationService),
        toastService,
    };
}

function createMeal(): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-17T12:30:00.000Z',
        mealType: 'SNACK',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: 52,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 14,
        totalFiber: 2,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        items: [],
        aiSessions: [],
    };
}
