import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { GoalsService } from '../../api/goals.service';

export type CalorieGoalDialogData = {
    dailyCalorieTarget?: number | null;
};

@Component({
    selector: 'fd-calorie-goal-dialog',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiDialogComponent, FdUiInputComponent, FdUiButtonComponent, FdUiDialogFooterDirective],
    templateUrl: './calorie-goal-dialog.html',
    styleUrls: ['./calorie-goal-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalorieGoalDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<CalorieGoalDialogComponent>);
    private readonly data = inject<CalorieGoalDialogData | null>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    private readonly goalsService = inject(GoalsService);

    protected readonly form = new FormGroup({
        dailyCalorieTarget: new FormControl<number | null>(null, [Validators.min(0)]),
    });

    public constructor() {
        if (this.data.dailyCalorieTarget !== undefined) {
            this.form.patchValue({ dailyCalorieTarget: this.data.dailyCalorieTarget ?? null });
        }
    }

    protected save(): void {
        this.form.markAllAsTouched();
        if (this.form.invalid) {
            return;
        }

        const payload = {
            dailyCalorieTarget: this.form.value.dailyCalorieTarget ?? null,
        };

        this.goalsService.updateGoals(payload).subscribe({
            next: result => {
                this.dialogRef.close(result !== null);
            },
            error: () => {
                this.dialogRef.close(false);
            },
        });
    }

    protected cancel(): void {
        this.dialogRef.close(false);
    }
}
