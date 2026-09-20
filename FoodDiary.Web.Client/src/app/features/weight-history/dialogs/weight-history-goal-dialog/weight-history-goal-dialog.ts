import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { MeasurementUnitPipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';

@Component({
    selector: 'fd-weight-history-goal-dialog',
    imports: [FdUiButtonComponent, FdUiDialogComponent, FdUiInputComponent, FormField, FormRoot, MeasurementUnitPipe, TranslatePipe],
    templateUrl: './weight-history-goal-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryGoalDialogComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly facade = inject(WeightHistoryFacade);
    private readonly dialogRef = inject(FdUiDialogRef<WeightHistoryGoalDialogComponent, void>);
    private readonly initialSaveVersion = this.facade.desiredWeightSaveVersion();

    protected readonly desiredWeightKg = this.facade.desiredWeightKg;
    protected readonly form = this.facade.desiredWeightForm;
    protected readonly weightField = this.facade.desiredWeightForm.weight;
    protected readonly isSaving = this.facade.isDesiredWeightSaving;

    public constructor() {
        effect(() => {
            if (this.facade.desiredWeightSaveVersion() > this.initialSaveVersion) {
                this.dialogRef.close();
            }
        });
    }

    protected submit(event: Event): void {
        event.preventDefault();
        this.save();
    }

    protected save(): void {
        if (this.isSaving() || this.form().invalid() || this.weightField().value().trim().length === 0) {
            return;
        }
        this.facade.saveDesiredWeight();
    }

    protected cancelGoal(): void {
        if (!this.isSaving()) {
            this.facade.cancelWeightGoal();
        }
    }

    protected close(): void {
        this.dialogRef.close();
    }
}
