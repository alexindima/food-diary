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
import { CHART_COLORS } from '../../../../constants/chart-colors';

export interface MacroGoalDialogData {
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
}

@Component({
    selector: 'fd-macro-goal-dialog',
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiDialogFooterDirective,
    ],
    templateUrl: './macro-goal-dialog.component.html',
    styleUrls: ['./macro-goal-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MacroGoalDialogComponent {
    private readonly dialogRef = inject(MatDialogRef<MacroGoalDialogComponent>);
    private readonly data = inject<MacroGoalDialogData | null>(MAT_DIALOG_DATA, { optional: true }) ?? {};
    private readonly userService = inject(UserService);
    private readonly fillAlpha = 0.08;
    public readonly nutrientFillColors = {
        protein: this.applyAlpha(CHART_COLORS.proteins, this.fillAlpha),
        fat: this.applyAlpha(CHART_COLORS.fats, this.fillAlpha),
        carb: this.applyAlpha(CHART_COLORS.carbs, this.fillAlpha),
        fiber: this.applyAlpha(CHART_COLORS.fiber, this.fillAlpha),
    };

    public readonly form = new FormGroup({
        proteinTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        fatTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        carbTarget: new FormControl<number | null>(null, [Validators.min(0)]),
        fiberTarget: new FormControl<number | null>(null, [Validators.min(0)]),
    });

    public constructor() {
        this.form.patchValue({
            proteinTarget: this.data.proteinTarget ?? null,
            fatTarget: this.data.fatTarget ?? null,
            carbTarget: this.data.carbTarget ?? null,
            fiberTarget: this.data.fiberTarget ?? null,
        });
    }

    public save(): void {
        this.form.markAllAsTouched();
        if (this.form.invalid) {
            return;
        }

        const payload = new UpdateUserDto({
            proteinTarget: this.form.value.proteinTarget ?? null,
            fatTarget: this.form.value.fatTarget ?? null,
            carbTarget: this.form.value.carbTarget ?? null,
            fiberTarget: this.form.value.fiberTarget ?? null,
        });

        this.userService.update(payload).subscribe({
            next: result => this.dialogRef.close(!!result),
            error: () => this.dialogRef.close(false),
        });
    }

    public cancel(): void {
        this.dialogRef.close(false);
    }

    private applyAlpha(hexColor: string, alpha: number): string {
        const normalized = hexColor.replace('#', '');
        const value = parseInt(normalized, 16);
        const r = (value >> 16) & 255;
        const g = (value >> 8) & 255;
        const b = value & 255;

        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
}
