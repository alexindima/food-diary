import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { GoalsService } from '../api/goals.service';
import type { GoalsResponse } from '../models/goals.data';
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

let facade: GoalsFacade;
let goalsService: { getGoals: ReturnType<typeof vi.fn>; updateGoals: ReturnType<typeof vi.fn> };
let toastService: { success: ReturnType<typeof vi.fn> };

describe('GoalsFacade', () => {
    beforeEach(() => {
        vi.useFakeTimers();

        goalsService = {
            getGoals: vi.fn().mockReturnValue(
                of({
                    dailyCalorieTarget: SAVED_CALORIES,
                    proteinTarget: SAVED_PROTEIN,
                    fatTarget: SAVED_FATS,
                    carbTarget: SAVED_CARBS,
                    fiberTarget: SAVED_FIBER,
                    waterGoal: SAVED_WATER,
                    desiredWeight: SAVED_WEIGHT,
                    desiredWaist: SAVED_WAIST,
                }),
            ),
            updateGoals: vi.fn().mockReturnValue(of({})),
        };
        toastService = {
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
    registerAutosaveTests();
});

function registerLoadTests(): void {
    describe('loading', () => {
        it('loads saved goals on initialize', () => {
            facade.initialize();

            expect(goalsService.getGoals).toHaveBeenCalledTimes(1);
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
                desiredWeight: AUTOSAVE_WEIGHT,
                desiredWaist: null,
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
            await Promise.resolve();
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

        it('preserves the latest queued payload when the in-flight autosave fails', async () => {
            const inFlightUpdate = new Subject<GoalsResponse | null>();
            goalsService.updateGoals.mockReturnValueOnce(inFlightUpdate.asObservable()).mockReturnValue(of({}));

            facade.updateCalories(IN_FLIGHT_CALORIES);
            await vi.advanceTimersByTimeAsync(AUTOSAVE_DELAY_MS);
            expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

            facade.updateWaterValue(RETRY_WATER);
            inFlightUpdate.error(new Error('save failed'));
            await Promise.resolve();
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
            expect(facade.saveStatusKey()).toBe('GOALS_PAGE.STATUS_ERROR');
            expect(toastService.success).not.toHaveBeenCalled();
        });
    });
}
