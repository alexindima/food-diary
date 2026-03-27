import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { UserService } from '../../../../services/user.service';
import { GoalsService } from '../../../../features/goals/api/goals.service';
import { FormGroupControls } from '../../../../types/common.data';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { UpdateUserDto } from '../../../../types/user.data';
import { forkJoin, of } from 'rxjs';

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
        FdUiInputComponent,
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
    private readonly goalsService = inject(GoalsService);

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
            stepGoal: this.form.value.stepGoal ?? undefined,
        });
        const hasStepGoal = this.form.value.stepGoal !== null && this.form.value.stepGoal !== undefined;
        const profileRequest$ = hasStepGoal ? this.userService.update(payload) : of({} as const);

        const goalsRequest$ = this.goalsService.updateGoals({
            dailyCalorieTarget: this.form.value.dailyCalorieTarget ?? null,
            proteinTarget: this.form.value.proteinTarget ?? null,
            fatTarget: this.form.value.fatTarget ?? null,
            carbTarget: this.form.value.carbTarget ?? null,
            waterGoal: this.form.value.waterGoal ?? null,
        });

        forkJoin([profileRequest$, goalsRequest$]).subscribe({
            next: ([profileResult, goalsResult]) => {
                const profileOk = !hasStepGoal || profileResult !== null;
                const goalsOk = goalsResult !== null;
                this.dialogRef.close(profileOk && goalsOk);
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

