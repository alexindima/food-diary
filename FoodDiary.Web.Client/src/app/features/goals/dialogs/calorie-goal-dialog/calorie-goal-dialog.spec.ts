import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { CalorieGoalFacade } from '../../lib/calorie-goal.facade';
import type { GoalsResponse } from '../../models/goals.data';
import { CalorieGoalDialogComponent, type CalorieGoalDialogData } from './calorie-goal-dialog';

const DEFAULT_CALORIE_TARGET = 2000;
const CURRENT_CALORIE_TARGET = 2500;
const UPDATED_CALORIE_TARGET = 1800;
const INVALID_CALORIE_TARGET = -100;
const DEFAULT_DIALOG_DATA = { dailyCalorieTarget: DEFAULT_CALORIE_TARGET };

let component: CalorieGoalDialogComponent;
let fixture: ComponentFixture<CalorieGoalDialogComponent>;
let calorieGoalFacadeSpy: { updateGoals: ReturnType<typeof vi.fn> };
let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

function createComponent(data: CalorieGoalDialogData | null = DEFAULT_DIALOG_DATA): void {
    calorieGoalFacadeSpy = { updateGoals: vi.fn() };
    dialogRefSpy = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [CalorieGoalDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: CalorieGoalFacade, useValue: calorieGoalFacadeSpy },
            { provide: FdUiDialogRef, useValue: dialogRefSpy },
            { provide: FD_UI_DIALOG_DATA, useValue: data },
        ],
    });

    fixture = TestBed.createComponent(CalorieGoalDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

describe('CalorieGoalDialogComponent', () => {
    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize form with current goal value', () => {
        createComponent({ dailyCalorieTarget: CURRENT_CALORIE_TARGET });
        expect(component['formModel']().dailyCalorieTarget).toBe(CURRENT_CALORIE_TARGET);
    });

    it('should initialize form with null when no data provided', () => {
        createComponent(null);
        expect(component['formModel']().dailyCalorieTarget).toBeNull();
    });

    it('should validate minimum calorie value', () => {
        createComponent();
        const field = component['form'].dailyCalorieTarget;
        field().value.set(INVALID_CALORIE_TARGET);
        field().markAsTouched();
        expect(field().invalid()).toBe(true);
        expect(
            field()
                .errors()
                .some(error => error.kind === 'min'),
        ).toBe(true);

        field().value.set(0);
        expect(field().invalid()).toBe(false);
    });

    it('should submit updated goal', () => {
        createComponent();
        const updatedGoals: GoalsResponse = { dailyCalorieTarget: UPDATED_CALORIE_TARGET, calorieCyclingEnabled: false };
        calorieGoalFacadeSpy.updateGoals.mockReturnValue(of(updatedGoals));

        component['form'].dailyCalorieTarget().value.set(UPDATED_CALORIE_TARGET);
        component['save']();

        expect(calorieGoalFacadeSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: UPDATED_CALORIE_TARGET });
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should prevent native form submit when saving', async () => {
        createComponent();
        const updatedGoals: GoalsResponse = { dailyCalorieTarget: UPDATED_CALORIE_TARGET, calorieCyclingEnabled: false };
        calorieGoalFacadeSpy.updateGoals.mockReturnValue(of(updatedGoals));
        component['form'].dailyCalorieTarget().value.set(UPDATED_CALORIE_TARGET);
        fixture.detectChanges();

        const form = (fixture.nativeElement as HTMLElement).querySelector('form');
        expect(form).not.toBeNull();

        const submitEvent = new Event('submit', { bubbles: true, cancelable: true });
        const wasNotCancelled = form?.dispatchEvent(submitEvent);
        await fixture.whenStable();

        expect(wasNotCancelled).toBe(false);
        expect(submitEvent.defaultPrevented).toBe(true);
        expect(calorieGoalFacadeSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: UPDATED_CALORIE_TARGET });
    });

    it('should close dialog with false on update error', () => {
        createComponent();
        calorieGoalFacadeSpy.updateGoals.mockReturnValue(throwError(() => new Error('fail')));

        component['form'].dailyCalorieTarget().value.set(UPDATED_CALORIE_TARGET);
        component['save']();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should close dialog on cancel', () => {
        createComponent();
        component['cancel']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should not submit when form is invalid', () => {
        createComponent();
        component['form'].dailyCalorieTarget().value.set(-1);
        component['save']();
        expect(calorieGoalFacadeSpy.updateGoals).not.toHaveBeenCalled();
    });

    it('should submit null when calorie target is cleared', () => {
        createComponent();
        const updatedGoals: GoalsResponse = { dailyCalorieTarget: null, calorieCyclingEnabled: false };
        calorieGoalFacadeSpy.updateGoals.mockReturnValue(of(updatedGoals));

        component['form'].dailyCalorieTarget().value.set(null);
        component['save']();

        expect(calorieGoalFacadeSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: null });
    });
});
