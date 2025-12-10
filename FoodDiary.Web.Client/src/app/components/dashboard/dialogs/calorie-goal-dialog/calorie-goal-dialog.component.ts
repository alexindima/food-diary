import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { UpdateUserDto } from '../../../../types/user.data';
import { UserService } from '../../../../services/user.service';

export interface CalorieGoalDialogData {
    dailyCalorieTarget?: number | null;
}

@Component({
    selector: 'fd-calorie-goal-dialog',
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiDialogFooterDirective,
    ],
    templateUrl: './calorie-goal-dialog.component.html',
    styleUrls: ['./calorie-goal-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalorieGoalDialogComponent {
    private readonly dialogRef = inject(MatDialogRef<CalorieGoalDialogComponent>);
    private readonly data = inject<CalorieGoalDialogData | null>(MAT_DIALOG_DATA, { optional: true }) ?? {};
    private readonly userService = inject(UserService);

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

        const payload = new UpdateUserDto({
            dailyCalorieTarget: this.form.value.dailyCalorieTarget ?? null,
        });

        this.userService.update(payload).subscribe({
            next: result => this.dialogRef.close(!!result),
            error: () => this.dialogRef.close(false),
        });
    }

    public cancel(): void {
        this.dialogRef.close(false);
    }
}
