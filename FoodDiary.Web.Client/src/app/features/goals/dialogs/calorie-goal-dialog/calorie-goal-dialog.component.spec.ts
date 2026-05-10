import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { GoalsService } from '../../api/goals.service';
import type { GoalsResponse } from '../../models/goals.data';
import { CalorieGoalDialogComponent, type CalorieGoalDialogData } from './calorie-goal-dialog.component';

describe('CalorieGoalDialogComponent', () => {
    let component: CalorieGoalDialogComponent;
    let fixture: ComponentFixture<CalorieGoalDialogComponent>;
    let goalsServiceSpy: { updateGoals: ReturnType<typeof vi.fn> };
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(data: CalorieGoalDialogData | null = { dailyCalorieTarget: 2000 }): void {
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
        createComponent({ dailyCalorieTarget: 2500 });
        expect(component.form.value.dailyCalorieTarget).toBe(2500);
    });

    it('should initialize form with null when no data provided', () => {
        createComponent(null);
        expect(component.form.value.dailyCalorieTarget).toBeNull();
    });

    it('should validate minimum calorie value', () => {
        createComponent();
        const control = component.form.controls.dailyCalorieTarget;
        control.setValue(-100);
        control.markAsTouched();
        expect(control.hasError('min')).toBe(true);

        control.setValue(0);
        expect(control.hasError('min')).toBe(false);
    });

    it('should submit updated goal', () => {
        createComponent();
        const updatedGoals: GoalsResponse = { dailyCalorieTarget: 1800, calorieCyclingEnabled: false };
        goalsServiceSpy.updateGoals.mockReturnValue(of(updatedGoals));

        component.form.controls.dailyCalorieTarget.setValue(1800);
        component.save();

        expect(goalsServiceSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: 1800 });
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should close dialog with false on update error', () => {
        createComponent();
        goalsServiceSpy.updateGoals.mockReturnValue(throwError(() => new Error('fail')));

        component.form.controls.dailyCalorieTarget.setValue(1800);
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
