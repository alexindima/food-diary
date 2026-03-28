import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CalorieGoalDialogComponent, CalorieGoalDialogData } from './calorie-goal-dialog.component';
import { GoalsService } from '../../api/goals.service';

describe('CalorieGoalDialogComponent', () => {
    let component: CalorieGoalDialogComponent;
    let fixture: ComponentFixture<CalorieGoalDialogComponent>;
    let goalsServiceSpy: any;
    let dialogRefSpy: any;

    function createComponent(data: CalorieGoalDialogData | null = { dailyCalorieTarget: 2000 }): void {
        goalsServiceSpy = { updateGoals: vi.fn() } as any;
        dialogRefSpy = { close: vi.fn() } as any;

        TestBed.configureTestingModule({
            imports: [CalorieGoalDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: GoalsService, useValue: goalsServiceSpy },
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: data },
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
        goalsServiceSpy.updateGoals.mockReturnValue(of({ dailyCalorieTarget: 1800 } as any));

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
        goalsServiceSpy.updateGoals.mockReturnValue(of({ dailyCalorieTarget: null } as any));

        component.form.controls.dailyCalorieTarget.setValue(null);
        component.save();

        expect(goalsServiceSpy.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: null });
    });
});
