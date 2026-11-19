import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import type { GoalsResponse } from '../../../shared/models/goals.data';
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
const CLASSIC_CALORIES = 2400;
const CLASSIC_PROTEIN = 180;
const CLASSIC_FATS = 80;
const CLASSIC_CARBS = 240;
const CUSTOM_PROTEIN = 160;
const AUTOSAVE_DELAY_MS = 700;
const AUTOSAVE_CALORIES = 2000;
const AUTOSAVE_PROTEIN = 140;
const AUTOSAVE_FATS = 60;
const AUTOSAVE_CARBS = 170;
const AUTOSAVE_FIBER = 25;
const AUTOSAVE_WATER = 2100;
const AUTOSAVE_WEIGHT = 70;
const IN_FLIGHT_CALORIES = 1800;
const QUEUED_WATER = 2500;
const RETRY_WATER = 2300;
const FAILED_SAVE_CALORIES = 1900;
const FAILED_MANUAL_WATER = 2210;
const LATEST_MANUAL_WATER = 2220;

let facade: GoalsFacade;
let goalsService: {
    getGoals: ReturnType<typeof vi.fn>;
    getGoalsStrict: ReturnType<typeof vi.fn>;
    updateGoals: ReturnType<typeof vi.fn<GoalsService['updateGoals']>>;
};
let toastService: { error: ReturnType<typeof vi.fn>; success: ReturnType<typeof vi.fn> };

describe('GoalsFacade', () => {
    beforeEach(() => {
        vi.useFakeTimers();

        goalsService = {
            getGoals: vi.fn(),
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

        TestBed.configureTestingModule({
            providers: [
                GoalsFacade,
                { provide: GoalsService, useValue: goalsService },
                { provide: FdUiToastService, useValue: toastService },
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

    afterEach(() => {
        vi.useRealTimers();
    });

    registerLoadTests();
    registerMacroTests();
    registerCalorieCyclingTests();
    registerInputTests();
    registerManualSaveTests();
    registerAutosaveTests();
    registerQueuedAutosaveRecoveryTests();
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

function registerMacroTests(): void {
    describe('macros', () => {
        it('recalculates macros for non-custom preset when calories change', () => {
            facade.changeMacroPreset('classic');
            facade.updateCalories(CLASSIC_CALORIES);

            expect(facade.selectedPreset()).toBe('classic');
            expect(facade.macroValues().protein).toBe(CLASSIC_PROTEIN);
            expect(facade.macroValues().fats).toBe(CLASSIC_FATS);
            expect(facade.macroValues().carbs).toBe(CLASSIC_CARBS);
        });

        it('switches preset to custom after manual macro edit', () => {
            facade.changeMacroPreset('classic');

            const clamped = facade.updateMacroValue('protein', CUSTOM_PROTEIN);

            expect(clamped).toBe(CUSTOM_PROTEIN);
            expect(facade.selectedPreset()).toBe('custom');
            expect(facade.macroValues().protein).toBe(CUSTOM_PROTEIN);
        });

        it('keeps fiber separate and reports macro and water shortfalls', () => {
            const lowProtein = 50;
            const fiberTarget = 32;
            const lowWater = 900;
            const waterInRange = 1_800;
            facade.updateMacroValue('protein', lowProtein);
            facade.updateMacroValue('fiber', fiberTarget);
            facade.updateWaterValue(lowWater);

            expect(facade.coreMacroStates().map(macro => macro.key)).toEqual(['protein', 'fats', 'carbs']);
            expect(facade.coreMacroStates()[0]).toMatchObject({ key: 'protein', value: lowProtein, shortfall: 30, inRange: false });
            expect(facade.fiberMacroState()).toMatchObject({ key: 'fiber', value: fiberTarget, shortfall: 0, inRange: true });
            expect(facade.waterState()).toMatchObject({ value: lowWater, shortfall: 300, inRange: false });

            facade.updateWaterValue(waterInRange);
            expect(facade.waterState()).toMatchObject({ value: waterInRange, shortfall: 0, inRange: true });
        });
    });
}

function registerCalorieCyclingTests(): void {
    describe('weekly calorie cycling', () => {
        it('starts an empty schedule from the daily goal and saves the edited day after the debounce', async () => {
            facade.initialize();
            const save = new Subject<GoalsResponse>();
            goalsService.updateGoals.mockReturnValueOnce(save);

            const fractionalMondayGoal = 2499.6;
            const roundedMondayGoal = 2500;
            facade.toggleCalorieCycling();
            facade.updateDayCalories('mondayCalories', fractionalMondayGoal);
            expect(facade.saveStatusKey()).toBe('GOALS_PAGE.STATUS_PENDING');
            expect(goalsService.updateGoals).not.toHaveBeenCalled();

            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(facade.saveStatusKey()).toBe('GOALS_PAGE.STATUS_SAVING');
            expect(goalsService.updateGoals).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({
                    calorieCyclingEnabled: true,
                    dailyCalorieTarget: SAVED_CALORIES,
                    mondayCalories: roundedMondayGoal,
                    tuesdayCalories: SAVED_CALORIES,
                    wednesdayCalories: SAVED_CALORIES,
                    thursdayCalories: SAVED_CALORIES,
                    fridayCalories: SAVED_CALORIES,
                    saturdayCalories: SAVED_CALORIES,
                    sundayCalories: SAVED_CALORIES,
                }),
            );
            save.next({ calorieCyclingEnabled: true, dailyCalorieTarget: SAVED_CALORIES, mondayCalories: roundedMondayGoal });
            save.complete();
            expect(facade.saveStatusKey()).toBeNull();
            expect(facade.dayCalories().mondayCalories).toBe(roundedMondayGoal);
        });

        it('keeps a loaded weekly schedule when re-enabled and omits day values when saved disabled', async () => {
            goalsService.getGoalsStrict.mockReturnValueOnce(
                of({ dailyCalorieTarget: SAVED_CALORIES, calorieCyclingEnabled: true, mondayCalories: 1800, sundayCalories: 2400 }),
            );
            facade.initialize();

            facade.toggleCalorieCycling();
            facade.toggleCalorieCycling();
            expect(facade.dayCalories()).toMatchObject({ mondayCalories: 1800, sundayCalories: 2400 });
            facade.toggleCalorieCycling();
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(goalsService.updateGoals).toHaveBeenCalledOnce();
            const request = goalsService.updateGoals.mock.calls[0][0];
            expect(request).toMatchObject({ calorieCyclingEnabled: false, dailyCalorieTarget: SAVED_CALORIES });
            expect(request).not.toHaveProperty('mondayCalories');
            expect(request).not.toHaveProperty('sundayCalories');
        });

        it('preserves zeros when cycling is enabled without a daily goal', () => {
            goalsService.getGoalsStrict.mockReturnValueOnce(of(null));
            facade.initialize();
            facade.toggleCalorieCycling();

            expect(facade.calorieCyclingEnabled()).toBe(true);
            expect(Object.values(facade.dayCalories())).toEqual([0, 0, 0, 0, 0, 0, 0]);
        });
    });
}

function registerInputTests(): void {
    describe('goal inputs', () => {
        it('ignores incomplete numeric edits without replacing saved values or scheduling an update', async () => {
            const savedMondayCalories = 1950;
            goalsService.getGoalsStrict.mockReturnValueOnce(
                of({
                    dailyCalorieTarget: SAVED_CALORIES,
                    calorieCyclingEnabled: true,
                    mondayCalories: savedMondayCalories,
                    waterGoal: SAVED_WATER,
                }),
            );
            facade.initialize();
            facade.updateCalories(Number.NaN);
            facade.updateDayCalories('mondayCalories', Number.NaN);

            expect(facade.updateMacroValue('protein', Number.NaN)).toBeNull();
            expect(facade.updateBodyTarget('weight', Number.NaN)).toBeNull();
            expect(facade.updateWaterValue(Number.NaN)).toBeNull();
            expect(facade.calorieTarget()).toBe(SAVED_CALORIES);
            expect(facade.dayCalories().mondayCalories).toBe(savedMondayCalories);
            expect(facade.waterValue()).toBe(SAVED_WATER);
            expect(facade.saveStatusKey()).toBeNull();
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);
            expect(goalsService.updateGoals).not.toHaveBeenCalled();
        });

        it('sends bounded calories and clears a nonpositive body target', async () => {
            const aboveCalorieLimit = 6500;
            const calorieLimit = 5000;
            const belowWaistMinimum = -20;
            facade.initialize();
            expect(facade.normalizeCaloriesInput(aboveCalorieLimit)).toBe(calorieLimit);
            expect(facade.updateBodyTarget('waist', belowWaistMinimum)).toBe(0);

            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(goalsService.updateGoals).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ dailyCalorieTarget: calorieLimit, desiredWeightKg: SAVED_WEIGHT, desiredWaistCm: null }),
            );
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
            expect(facade.hasAutosaveError()).toBe(true);
            expect(await facade.saveManuallyAsync({ waterGoal: LATEST_MANUAL_WATER })).toBe(true);
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS * 2);

            expect(goalsService.updateGoals.mock.calls).toEqual([
                [{ waterGoal: FAILED_MANUAL_WATER }],
                [{ waterGoal: LATEST_MANUAL_WATER }],
            ]);
            expect(facade.waterValue()).toBe(LATEST_MANUAL_WATER);
            expect(facade.hasPendingAutosave()).toBe(false);
            expect(facade.hasAutosaveError()).toBe(false);
            expect(facade.isSavingGoals()).toBe(false);
        });

        it('supersedes a queued autosave with the submitted manual value', async () => {
            facade.initialize();
            facade.updateWaterValue(FAILED_MANUAL_WATER);
            goalsService.updateGoals.mockReturnValueOnce(of({ waterGoal: LATEST_MANUAL_WATER, calorieCyclingEnabled: false }));

            expect(await facade.saveManuallyAsync({ waterGoal: LATEST_MANUAL_WATER })).toBe(true);
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS * 2);

            expect(goalsService.updateGoals).toHaveBeenCalledExactlyOnceWith({ waterGoal: LATEST_MANUAL_WATER });
            expect(facade.waterValue()).toBe(LATEST_MANUAL_WATER);
            expect(facade.hasPendingAutosave()).toBe(false);
        });

        it('applies the saved response when a manual save succeeds', () => {
            goalsService.updateGoals.mockReturnValueOnce(
                of({
                    dailyCalorieTarget: AUTOSAVE_CALORIES,
                    proteinTarget: AUTOSAVE_PROTEIN,
                    fatTarget: AUTOSAVE_FATS,
                    carbTarget: AUTOSAVE_CARBS,
                    fiberTarget: AUTOSAVE_FIBER,
                    waterGoal: AUTOSAVE_WATER,
                    desiredWeightKg: AUTOSAVE_WEIGHT,
                    desiredWaistCm: null,
                    calorieCyclingEnabled: false,
                }),
            );

            void facade.saveManuallyAsync({ dailyCalorieTarget: AUTOSAVE_CALORIES });

            expect(facade.calorieTarget()).toBe(AUTOSAVE_CALORIES);
            expect(facade.macroValues()).toEqual({
                protein: AUTOSAVE_PROTEIN,
                fats: AUTOSAVE_FATS,
                carbs: AUTOSAVE_CARBS,
                fiber: AUTOSAVE_FIBER,
            });
            expect(facade.bodyTargetValues()).toEqual({ weight: AUTOSAVE_WEIGHT, waist: 0 });
            expect(toastService.success).toHaveBeenCalledWith('GOALS_PAGE.SAVED_TOAST');
        });
    });
}

function registerAutosaveTests(): void {
    describe('autosave', () => {
        it('debounces autosave and sends the latest payload', async () => {
            facade.updateCalories(AUTOSAVE_CALORIES);
            facade.updateMacroValue('protein', AUTOSAVE_PROTEIN);
            facade.updateMacroValue('fats', AUTOSAVE_FATS);
            facade.updateMacroValue('carbs', AUTOSAVE_CARBS);
            facade.updateMacroValue('fiber', AUTOSAVE_FIBER);
            facade.updateWaterValue(AUTOSAVE_WATER);
            facade.updateBodyTarget('weight', AUTOSAVE_WEIGHT);
            facade.updateBodyTarget('waist', 0);

            expect(goalsService.updateGoals).not.toHaveBeenCalled();

            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(goalsService.updateGoals).toHaveBeenCalledWith({
                dailyCalorieTarget: AUTOSAVE_CALORIES,
                proteinTarget: AUTOSAVE_PROTEIN,
                fatTarget: AUTOSAVE_FATS,
                carbTarget: AUTOSAVE_CARBS,
                fiberTarget: AUTOSAVE_FIBER,
                waterGoal: AUTOSAVE_WATER,
                desiredWeightKg: AUTOSAVE_WEIGHT,
                desiredWaistCm: null,
                calorieCyclingEnabled: false,
            });
            expect(toastService.success).toHaveBeenCalledWith('GOALS_PAGE.SAVED_TOAST');
        });

        it('queues the latest autosave payload while a save is in flight', async () => {
            const inFlightUpdate = new Subject<GoalsResponse | null>();
            goalsService.updateGoals.mockReturnValueOnce(inFlightUpdate.asObservable());

            facade.updateCalories(IN_FLIGHT_CALORIES);
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);
            expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

            facade.updateWaterValue(QUEUED_WATER);
            expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

            inFlightUpdate.next({ calorieCyclingEnabled: false });
            inFlightUpdate.complete();
            await waitForAsyncTasksAsync();
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(goalsService.updateGoals).toHaveBeenCalledTimes(2);
            expect(goalsService.updateGoals.mock.calls[1][0]).toEqual(
                expect.objectContaining({
                    dailyCalorieTarget: IN_FLIGHT_CALORIES,
                    waterGoal: QUEUED_WATER,
                }),
            );
            expect(toastService.success).toHaveBeenCalledTimes(2);
        });
    });
}

function registerQueuedAutosaveRecoveryTests(): void {
    describe('recovering autosave', () => {
        it.each(['error', 'empty result'] as const)('preserves the latest queued payload after an in-flight %s', async result => {
            const inFlightUpdate = new Subject<GoalsResponse | null>();
            goalsService.updateGoals.mockReturnValueOnce(inFlightUpdate.asObservable());

            facade.updateCalories(IN_FLIGHT_CALORIES);
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);
            expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

            facade.updateWaterValue(RETRY_WATER);
            if (result === 'error') {
                inFlightUpdate.error(new Error('save failed'));
            } else {
                inFlightUpdate.next(null);
                inFlightUpdate.complete();
            }
            await waitForAsyncTasksAsync();
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(goalsService.updateGoals).toHaveBeenCalledTimes(2);
            expect(goalsService.updateGoals.mock.calls[1][0]).toEqual(
                expect.objectContaining({
                    dailyCalorieTarget: IN_FLIGHT_CALORIES,
                    waterGoal: RETRY_WATER,
                }),
            );
        });

        it('surfaces autosave error state when the save fails without newer changes', async () => {
            facade.initialize();
            goalsService.updateGoals.mockReturnValueOnce(throwError(() => new Error('save failed')));

            facade.updateCalories(FAILED_SAVE_CALORIES);
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);

            expect(facade.hasAutosaveError()).toBe(true);
            expect(facade.saveStatusKey()).toBeNull();
            expect(toastService.error).toHaveBeenCalledWith('GOALS_PAGE.STATUS_ERROR');
            expect(toastService.success).not.toHaveBeenCalled();
        });
    });
}
