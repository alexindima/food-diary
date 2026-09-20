import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { MeasurementUnitPipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';

@Component({
    selector: 'fd-waist-history-goal-dialog',
    imports: [FdUiButtonComponent, FdUiDialogComponent, FdUiInputComponent, FormField, FormRoot, MeasurementUnitPipe, TranslatePipe],
    templateUrl: './waist-history-goal-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryGoalDialogComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly facade = inject(WaistHistoryFacade);
    private readonly dialogRef = inject(FdUiDialogRef<WaistHistoryGoalDialogComponent, void>);
    private readonly initialSaveVersion = this.facade.desiredWaistSaveVersion();
    protected readonly desiredWaistCm = this.facade.desiredWaistCm;
    protected readonly form = this.facade.desiredWaistForm;
    protected readonly waistField = this.facade.desiredWaistForm.circumference;
    protected readonly isSaving = this.facade.isDesiredWaistSaving;

    public constructor() {
        effect(() => {
            if (this.facade.desiredWaistSaveVersion() > this.initialSaveVersion) {
                this.dialogRef.close();
            }
        });
    }

    protected submit(event: Event): void {
        event.preventDefault();
        this.save();
    }

    protected save(): void {
        if (this.isSaving() || this.form().invalid() || this.waistField().value().trim().length === 0) {
            return;
        }
        this.facade.saveDesiredWaist();
    }

    protected cancelGoal(): void {
        if (!this.isSaving()) {
            this.facade.cancelWaistGoal();
        }
    }

    protected close(): void {
        this.dialogRef.close();
    }
}
