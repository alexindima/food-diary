import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, min } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { CalorieGoalFacade } from '../../lib/calorie-goal.facade';

export type CalorieGoalDialogData = {
    dailyCalorieTarget?: number | null;
};

@Component({
    selector: 'fd-calorie-goal-dialog',
    imports: [FormField, FormRoot, TranslatePipe, FdUiDialogComponent, FdUiInputComponent, FdUiButtonComponent, FdUiDialogFooterDirective],
    templateUrl: './calorie-goal-dialog.html',
    styleUrls: ['./calorie-goal-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalorieGoalDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<CalorieGoalDialogComponent>);
    private readonly data = inject<CalorieGoalDialogData | null>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};
    private readonly calorieGoalFacade = inject(CalorieGoalFacade);

    protected readonly formModel = signal({
        dailyCalorieTarget: this.data.dailyCalorieTarget ?? null,
    });
    private readonly submitCalorieGoalFormAsync = async (): Promise<void> => {
        await this.saveAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            min(path.dailyCalorieTarget, 0);
        },
        {
            submission: {
                action: this.submitCalorieGoalFormAsync,
            },
        },
    );

    protected save(): void {
        void this.saveAsync();
    }

    private async saveAsync(): Promise<void> {
        this.form().markAsTouched();
        if (this.form().invalid()) {
            return;
        }

        const payload = {
            dailyCalorieTarget: this.formModel().dailyCalorieTarget ?? null,
        };

        try {
            const result = await firstValueFrom(this.calorieGoalFacade.updateGoals(payload));
            this.dialogRef.close(result !== null);
        } catch {
            this.dialogRef.close(false);
        }
    }

    protected cancel(): void {
        this.dialogRef.close(false);
    }
}
