import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { GoalsService } from '../../api/goals.service';

export interface CalorieGoalDialogData {
    dailyCalorieTarget?: number | null;
}

@Component({
    selector: 'fd-calorie-goal-dialog',
    standalone: true,
    imports: [ReactiveFormsModule, TranslatePipe, FdUiDialogComponent, FdUiInputComponent, FdUiButtonComponent, FdUiDialogFooterDirective],
    templateUrl: './calorie-goal-dialog.component.html',
    styleUrls: ['./calorie-goal-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalorieGoalDialogComponent {
    private readonly dialogRef = inject(MatDialogRef<CalorieGoalDialogComponent>);
    private readonly data = inject<CalorieGoalDialogData | null>(MAT_DIALOG_DATA, { optional: true }) ?? {};
    private readonly goalsService = inject(GoalsService);

    public readonly form = new FormGroup({
        dailyCalorieTarget: new FormControl<number | null>(null, [Validators.min(0)]),
    });

    public constructor() {
        if (this.data.dailyCalorieTarget !== undefined) {
            this.form.patchValue({ dailyCalorieTarget: this.data.dailyCalorieTarget ?? null });
        }
    }

    public save(): void {
        this.form.markAllAsTouched();
        if (this.form.invalid) {
            return;
        }

        const payload = {
            dailyCalorieTarget: this.form.value.dailyCalorieTarget ?? null,
        };

        this.goalsService.updateGoals(payload).subscribe({
            next: result => this.dialogRef.close(result !== null),
            error: () => this.dialogRef.close(false),
        });
    }

    public cancel(): void {
        this.dialogRef.close(false);
    }
}
