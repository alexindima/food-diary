import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { GoalsResponse } from '../../../shared/models/goals.data';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { GoalsService } from '../api/goals.service';
import { GoalsFacade } from './goals.facade';

const SAVED_CALORIES = 2100;
const SAVED_PROTEIN = 150;
const SAVED_FATS = 70;
const SAVED_CARBS = 180;
const SAVED_FIBER = 30;
const SAVED_WATER = 2200;
const SAVED_WEIGHT = 72;
const SAVED_WAIST = 80;
const SAVED_MANUAL_WATER = 2400;
const FAILED_MANUAL_WATER = 2210;
const LATEST_MANUAL_WATER = 2220;

let facade: GoalsFacade;
let goalsService: {
    getGoalsStrict: ReturnType<typeof vi.fn>;
    updateGoals: ReturnType<typeof vi.fn<GoalsService['updateGoals']>>;
};
let toastService: { error: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };

let reportChange: ReturnType<typeof vi.fn<NutritionDataInvalidationService['reportGoalMutation']>>;

describe('GoalsFacade', () => {
    beforeEach(() => {
        goalsService = {
            getGoalsStrict: vi.fn().mockReturnValue(
                of({
                    dailyCalorieTarget: SAVED_CALORIES,
                    proteinTarget: SAVED_PROTEIN,
                    fatTarget: SAVED_FATS,
                    carbTarget: SAVED_CARBS,
                    fiberTarget: SAVED_FIBER,
                    waterGoal: SAVED_WATER,
                    desiredWeightKg: SAVED_WEIGHT,
                    desiredWaistCm: SAVED_WAIST,
                }),
            ),
            updateGoals: vi.fn<GoalsService['updateGoals']>().mockReturnValue(of({ calorieCyclingEnabled: false })),
        };
        toastService = {
            error: vi.fn(),
            success: vi.fn(),
        };

        reportChange = vi.fn<NutritionDataInvalidationService['reportGoalMutation']>();
        TestBed.configureTestingModule({
            providers: [
                GoalsFacade,
                { provide: GoalsService, useValue: goalsService },
                { provide: FdUiToastService, useValue: toastService },
                { provide: NutritionDataInvalidationService, useValue: { reportGoalMutation: reportChange } },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                    },
                },
            ],
        });

        facade = TestBed.inject(GoalsFacade);
    });

    registerLoadTests();
    registerManualSaveTests();
});

function registerLoadTests(): void {
    describe('loading', () => {
        it('loads saved goals on initialize', () => {
            facade.initialize();

            expect(goalsService.getGoalsStrict).toHaveBeenCalledTimes(1);
            expect(facade.calorieTarget()).toBe(SAVED_CALORIES);
            expect(facade.macroValues()).toEqual({
                protein: SAVED_PROTEIN,
                fats: SAVED_FATS,
                carbs: SAVED_CARBS,
                fiber: SAVED_FIBER,
            });
            expect(facade.waterValue()).toBe(SAVED_WATER);
            expect(facade.bodyTargetValues()).toEqual({
                weight: SAVED_WEIGHT,
                waist: SAVED_WAIST,
            });
        });

        it('clears loaded numeric goals when backend returns null values', () => {
            facade.initialize();
            goalsService.getGoalsStrict.mockReturnValueOnce(
                of({
                    dailyCalorieTarget: null,
                    proteinTarget: null,
                    fatTarget: null,
                    carbTarget: null,
                    fiberTarget: null,
                    waterGoal: null,
                    desiredWeightKg: null,
                    desiredWaistCm: null,
                    calorieCyclingEnabled: false,
                }),
            );

            facade.reload();

            expect(facade.calorieTarget()).toBe(0);
            expect(facade.macroValues()).toEqual({
                protein: 0,
                fats: 0,
                carbs: 0,
                fiber: 0,
            });
            expect(facade.waterValue()).toBe(0);
            expect(facade.bodyTargetValues()).toEqual({
                weight: 0,
                waist: 0,
            });
        });

        it('keeps existing goals and marks load error when reload fails', () => {
            facade.initialize();
            goalsService.getGoalsStrict.mockReturnValueOnce(throwError(() => new Error('load failed')));

            facade.reload();

            expect(facade.hasLoadError()).toBe(true);
            expect(facade.calorieTarget()).toBe(SAVED_CALORIES);
            expect(facade.macroValues()).toEqual({
                protein: SAVED_PROTEIN,
                fats: SAVED_FATS,
                carbs: SAVED_CARBS,
                fiber: SAVED_FIBER,
            });
            expect(facade.waterValue()).toBe(SAVED_WATER);
            expect(facade.bodyTargetValues()).toEqual({
                weight: SAVED_WEIGHT,
                waist: SAVED_WAIST,
            });
        });
    });
}

function registerManualSaveTests(): void {
    describe('manual save', () => {
        it.each(['error', 'null'] as const)('keeps the latest manual save after a previous %s response', async failure => {
            facade.initialize();
            const failedResponse = failure === 'error' ? throwError(() => new Error('save failed')) : of(null);
            goalsService.updateGoals
                .mockReturnValueOnce(failedResponse)
                .mockReturnValueOnce(of({ waterGoal: LATEST_MANUAL_WATER, calorieCyclingEnabled: false }));

            expect(await facade.saveManuallyAsync({ waterGoal: FAILED_MANUAL_WATER })).toBe(false);
            expect(toastService.error).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.STATUS_ERROR');
            expect(facade.waterValue()).toBe(SAVED_WATER);
            expect(reportChange).not.toHaveBeenCalled();
            expect(await facade.saveManuallyAsync({ waterGoal: LATEST_MANUAL_WATER })).toBe(true);

            expect(goalsService.updateGoals.mock.calls).toEqual([
                [{ waterGoal: FAILED_MANUAL_WATER }],
                [{ waterGoal: LATEST_MANUAL_WATER }],
            ]);
            expect(facade.waterValue()).toBe(LATEST_MANUAL_WATER);
            expect(reportChange).toHaveBeenCalledOnce();
            expect(toastService.success).toHaveBeenCalledExactlyOnceWith('GOALS_PAGE.SAVED_TOAST');
            expect(facade.isSavingGoals()).toBe(false);
        });

        it('applies the normalized server response when a manual save succeeds', async () => {
            facade.initialize();
            const pending = new Subject<GoalsResponse>();
            goalsService.updateGoals.mockReturnValueOnce(pending);
            const saved = facade.saveManuallyAsync({ dailyCalorieTarget: SAVED_CALORIES, waterGoal: SAVED_MANUAL_WATER });
            expect(facade.saveStatusKey()).toBe('GOALS_PAGE.STATUS_SAVING');
            expect(facade.waterValue()).toBe(SAVED_WATER);

            pending.next({
                dailyCalorieTarget: SAVED_CALORIES,
                proteinTarget: SAVED_PROTEIN,
                fatTarget: SAVED_FATS,
                carbTarget: SAVED_CARBS,
                fiberTarget: SAVED_FIBER,
                waterGoal: LATEST_MANUAL_WATER,
                desiredWeightKg: SAVED_WEIGHT,
                desiredWaistCm: null,
                calorieCyclingEnabled: false,
            });
            pending.complete();

            expect(await saved).toBe(true);
            expect(facade.waterValue()).toBe(LATEST_MANUAL_WATER);
            expect(facade.bodyTargetValues()).toEqual({ weight: SAVED_WEIGHT, waist: 0 });
            expect(facade.saveStatusKey()).toBeNull();
            expect(toastService.success).toHaveBeenCalledWith('GOALS_PAGE.SAVED_TOAST');
        });

        it.each([true, false])('settles a submitted save after its owner is destroyed, success=%s', async success => {
            facade.initialize();
            const pending = new Subject<GoalsResponse | null>();
            goalsService.updateGoals.mockReturnValueOnce(pending);
            const saved = facade.saveManuallyAsync({ waterGoal: LATEST_MANUAL_WATER });

            TestBed.resetTestingModule();
            expect(pending.observed).toBe(true);
            pending.next(success ? { waterGoal: LATEST_MANUAL_WATER, calorieCyclingEnabled: false } : null);
            pending.complete();

            expect(await saved).toBe(success);
            expect(reportChange).toHaveBeenCalledTimes(success ? 1 : 0);
            expect(toastService.success).not.toHaveBeenCalled();
            expect(toastService.error).not.toHaveBeenCalled();
        });
    });
}
