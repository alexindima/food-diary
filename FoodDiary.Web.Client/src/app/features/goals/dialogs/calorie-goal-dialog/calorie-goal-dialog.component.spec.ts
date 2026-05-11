import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { GoalsService } from '../../api/goals.service';
import type { GoalsResponse } from '../../models/goals.data';
import { CalorieGoalDialogComponent, type CalorieGoalDialogData } from './calorie-goal-dialog.component';

const DEFAULT_CALORIE_TARGET = 2000;
const CURRENT_CALORIE_TARGET = 2500;
const UPDATED_CALORIE_TARGET = 1800;
const INVALID_CALORIE_TARGET = -100;

describe('CalorieGoalDialogComponent', () => {
    let component: CalorieGoalDialogComponent;
    let fixture: ComponentFixture<CalorieGoalDialogComponent>;
    let goalsServiceSpy: { updateGoals: ReturnType<typeof vi.fn> };
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: CalorieGoalDialogData | null = { dailyCalorieTarget: DEFAULT_CALORIE_TARGET }): void {
        goalsServiceSpy = { updateGoals: vi.fn() };
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [CalorieGoalDialogComponent, TranslateModule.forRoot()],
            providers: [
                { provide: GoalsService, useValue: goalsServiceSpy },
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        fixture = TestBed.createComponent(CalorieGoalDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize form with current goal value', () => {
        createComponent({ dailyCalorieTarget: CURRENT_CALORIE_TARGET });
        expect(component.form.value.dailyCalorieTarget).toBe(CURRENT_CALORIE_TARGET);
    });

    it('should initialize form with null when no data provided', () => {
        createComponent(null);
        expect(component.form.value.dailyCalorieTarget).toBeNull();
    });

    it('should validate minimum calorie value', () => {
        createComponent();
        const control = component.form.controls.dailyCalorieTarget;
        control.setValue(INVALID_CALORIE_TARGET);
        control.markAsTouched();
        expect(control.hasError('min')).toBe(true);

        control.setValue(0);
        expect(control.hasError('min')).toBe(false);
    });

    it('should submit updated goal', () => {
        createComponent();
        const updatedGoals: GoalsResponse = { dailyCalorieTarget: UPDATED_CALORIE_TARGET, calorieCyclingEnabled: false };
        goalsServiceSpy.updateGoals.mockReturnValue(of(updatedGoals));

        component.form.controls.dailyCalorieTarget.setValue(UPDATED_CALORIE_TARGET);
        component.save();

        expect(goalsServiceSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: UPDATED_CALORIE_TARGET });
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should close dialog with false on update error', () => {
        createComponent();
        goalsServiceSpy.updateGoals.mockReturnValue(throwError(() => new Error('fail')));

        component.form.controls.dailyCalorieTarget.setValue(UPDATED_CALORIE_TARGET);
        component.save();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should close dialog on cancel', () => {
        createComponent();
        component.cancel();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should not submit when form is invalid', () => {
        createComponent();
        component.form.controls.dailyCalorieTarget.setValue(-1);
        component.save();
        expect(goalsServiceSpy.updateGoals).not.toHaveBeenCalled();
    });

    it('should submit null when calorie target is cleared', () => {
        createComponent();
        const updatedGoals: GoalsResponse = { dailyCalorieTarget: null, calorieCyclingEnabled: false };
        goalsServiceSpy.updateGoals.mockReturnValue(of(updatedGoals));

        component.form.controls.dailyCalorieTarget.setValue(null);
        component.save();

        expect(goalsServiceSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: null });
    });
});
