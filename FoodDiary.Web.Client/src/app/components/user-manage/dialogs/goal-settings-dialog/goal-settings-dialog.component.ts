import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiPlainInputComponent } from 'fd-ui-kit/plain-input/fd-ui-plain-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { UserService } from '../../../../services/user.service';
import { FormGroupControls } from '../../../../types/common.data';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { UpdateUserDto } from '../../../../types/user.data';

export interface GoalSettingsData {
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    stepGoal?: number | null;
    waterGoal?: number | null;
}

@Component({
    selector: 'fd-goal-settings-dialog',
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiPlainInputComponent,
        FdUiButtonComponent,
        FdUiDialogFooterDirective,
    ],
    templateUrl: './goal-settings-dialog.component.html',
    styleUrls: ['./goal-settings-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalSettingsDialogComponent {
    private readonly dialogRef = inject(MatDialogRef<GoalSettingsDialogComponent>);
    private readonly dialogData = inject<GoalSettingsData | null>(MAT_DIALOG_DATA, { optional: true }) ?? {};
    private readonly userService = inject(UserService);

    public readonly form = new FormGroup<GoalFormData>({
        dailyCalorieTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        proteinTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        fatTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        carbTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        stepGoal: new FormControl<number | null>(null, [Validators.min(0)]),
        waterGoal: new FormControl<number | null>(null, [Validators.min(0)]),
    });

    public constructor() {
        const initial = this.dialogData;
        if (initial) {
            this.form.patchValue(initial);
        }
    }

    public onSubmit(): void {
        this.form.markAllAsTouched();
        if (this.form.invalid) {
            return;
        }

        const payload = new UpdateUserDto({
            dailyCalorieTarget: this.form.value.dailyCalorieTarget ?? undefined,
            proteinTarget: this.form.value.proteinTarget ?? undefined,
            fatTarget: this.form.value.fatTarget ?? undefined,
            carbTarget: this.form.value.carbTarget ?? undefined,
            stepGoal: this.form.value.stepGoal ?? undefined,
            waterGoal: this.form.value.waterGoal ?? undefined,
        });

        this.userService.update(payload).subscribe({
            next: success => {
                if (success) {
                    this.dialogRef.close(true);
                } else {
                    this.dialogRef.close(false);
                }
            },
            error: () => {
                this.dialogRef.close(false);
            },
        });
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}

type GoalFormValues = {
    dailyCalorieTarget: number | null;
    proteinTarget: number | null;
    fatTarget: number | null;
    carbTarget: number | null;
    stepGoal: number | null;
    waterGoal: number | null;
};

type GoalFormData = FormGroupControls<GoalFormValues>;
