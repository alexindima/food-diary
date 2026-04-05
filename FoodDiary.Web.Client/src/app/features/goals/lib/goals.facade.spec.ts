import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { GoalsService } from '../api/goals.service';
import { GoalsFacade } from './goals.facade';

describe('GoalsFacade', () => {
    let facade: GoalsFacade;
    let goalsService: { getGoals: ReturnType<typeof vi.fn>; updateGoals: ReturnType<typeof vi.fn> };

    beforeEach(() => {
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
            updateGoals: vi.fn().mockReturnValue(of(null)),
        };

        TestBed.configureTestingModule({
            providers: [GoalsFacade, { provide: GoalsService, useValue: goalsService }],
        });

        facade = TestBed.inject(GoalsFacade);
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

    it('saves normalized goals payload', () => {
        facade.updateCalories(2000);
        facade.updateMacroValue('protein', 140);
        facade.updateMacroValue('fats', 60);
        facade.updateMacroValue('carbs', 170);
        facade.updateMacroValue('fiber', 25);
        facade.updateWaterValue(2100);
        facade.updateBodyTarget('weight', 70);
        facade.updateBodyTarget('waist', 0);

        facade.saveGoals();

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
    });
});
