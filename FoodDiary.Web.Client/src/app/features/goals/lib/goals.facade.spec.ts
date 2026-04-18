import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { Subject, of, throwError } from 'rxjs';
import { GoalsService } from '../api/goals.service';
import { GoalsFacade } from './goals.facade';

describe('GoalsFacade', () => {
    let facade: GoalsFacade;
    let goalsService: { getGoals: ReturnType<typeof vi.fn>; updateGoals: ReturnType<typeof vi.fn> };
    let toastService: { success: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        vi.useFakeTimers();

        goalsService = {
            getGoals: vi.fn().mockReturnValue(
                of({
                    dailyCalorieTarget: 2100,
                    proteinTarget: 150,
                    fatTarget: 70,
                    carbTarget: 180,
                    fiberTarget: 30,
                    waterGoal: 2200,
                    desiredWeight: 72,
                    desiredWaist: 80,
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

    it('loads saved goals on initialize', () => {
        facade.initialize();

        expect(goalsService.getGoals).toHaveBeenCalledTimes(1);
        expect(facade.calorieTarget()).toBe(2100);
        expect(facade.macroValues()).toEqual({
            protein: 150,
            fats: 70,
            carbs: 180,
            fiber: 30,
        });
        expect(facade.waterValue()).toBe(2200);
        expect(facade.bodyTargetValues()).toEqual({
            weight: 72,
            waist: 80,
        });
    });

    it('recalculates macros for non-custom preset when calories change', () => {
        facade.changeMacroPreset('classic');
        facade.updateCalories(2400);

        expect(facade.selectedPreset()).toBe('classic');
        expect(facade.macroValues().protein).toBe(180);
        expect(facade.macroValues().fats).toBe(80);
        expect(facade.macroValues().carbs).toBe(240);
    });

    it('switches preset to custom after manual macro edit', () => {
        facade.changeMacroPreset('classic');

        const clamped = facade.updateMacroValue('protein', 160);

        expect(clamped).toBe(160);
        expect(facade.selectedPreset()).toBe('custom');
        expect(facade.macroValues().protein).toBe(160);
    });

    it('debounces autosave and sends the latest payload', async () => {
        facade.updateCalories(2000);
        facade.updateMacroValue('protein', 140);
        facade.updateMacroValue('fats', 60);
        facade.updateMacroValue('carbs', 170);
        facade.updateMacroValue('fiber', 25);
        facade.updateWaterValue(2100);
        facade.updateBodyTarget('weight', 70);
        facade.updateBodyTarget('waist', 0);

        expect(goalsService.updateGoals).not.toHaveBeenCalled();

        await vi.advanceTimersByTimeAsync(700);

        expect(goalsService.updateGoals).toHaveBeenCalledWith({
            dailyCalorieTarget: 2000,
            proteinTarget: 140,
            fatTarget: 60,
            carbTarget: 170,
            fiberTarget: 25,
            waterGoal: 2100,
            desiredWeight: 70,
            desiredWaist: null,
            calorieCyclingEnabled: false,
        });
        expect(toastService.success).toHaveBeenCalledWith('GOALS_PAGE.SAVED_TOAST');
    });

    it('queues the latest autosave payload while a save is in flight', async () => {
        const inFlightUpdate = new Subject<any>();
        goalsService.updateGoals.mockReturnValueOnce(inFlightUpdate.asObservable());

        facade.updateCalories(1800);
        await vi.advanceTimersByTimeAsync(700);
        expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

        facade.updateWaterValue(2500);
        expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

        inFlightUpdate.next({});
        inFlightUpdate.complete();
        await Promise.resolve();
        await vi.advanceTimersByTimeAsync(700);

        expect(goalsService.updateGoals).toHaveBeenCalledTimes(2);
        expect(goalsService.updateGoals.mock.calls[1][0]).toEqual(
            expect.objectContaining({
                dailyCalorieTarget: 1800,
                waterGoal: 2500,
            }),
        );
        expect(toastService.success).toHaveBeenCalledTimes(2);
    });

    it('preserves the latest queued payload when the in-flight autosave fails', async () => {
        const inFlightUpdate = new Subject<any>();
        goalsService.updateGoals.mockReturnValueOnce(inFlightUpdate.asObservable()).mockReturnValue(of({}));

        facade.updateCalories(1800);
        await vi.advanceTimersByTimeAsync(700);
        expect(goalsService.updateGoals).toHaveBeenCalledTimes(1);

        facade.updateWaterValue(2300);
        inFlightUpdate.error(new Error('save failed'));
        await Promise.resolve();
        await vi.advanceTimersByTimeAsync(700);

        expect(goalsService.updateGoals).toHaveBeenCalledTimes(2);
        expect(goalsService.updateGoals.mock.calls[1][0]).toEqual(
            expect.objectContaining({
                dailyCalorieTarget: 1800,
                waterGoal: 2300,
            }),
        );
    });

    it('surfaces autosave error state when the save fails without newer changes', async () => {
        facade.initialize();
        goalsService.updateGoals.mockReturnValueOnce(throwError(() => new Error('save failed')));

        facade.updateCalories(1900);
        await vi.advanceTimersByTimeAsync(700);

        expect(facade.hasAutosaveError()).toBe(true);
        expect(facade.saveStatusKey()).toBe('GOALS_PAGE.STATUS_ERROR');
        expect(toastService.success).not.toHaveBeenCalled();
    });
});
