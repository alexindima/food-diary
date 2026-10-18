import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { connectMeasurementDevelopment } from '../../../../shared/development/measurement-development';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightHistoryFormCardComponent } from '../../components/weight-history-form-card/weight-history-form-card';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';

@Component({
    selector: 'fd-weight-history-entry-dialog',
    imports: [TranslatePipe, FdUiDialogComponent, WeightHistoryFormCardComponent],
    templateUrl: './weight-history-entry-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryEntryDialogComponent {
    private readonly facade = inject(WeightHistoryFacade);
    private readonly dialogRef = inject(FdUiDialogRef<WeightHistoryEntryDialogComponent, void>);
    private readonly initialSaveVersion = this.facade.entrySaveVersion();

    protected readonly form = this.facade.form;
    protected readonly isSaving = this.facade.isSaving;
    protected readonly isEditing = this.facade.isEditing;
    protected readonly error = this.facade.entryError;

    public constructor() {
        const measurements = inject(MeasurementSystemService);
        connectMeasurementDevelopment({
            kind: 'weight',
            close: () => {
                this.close();
            },
            read: () => ({
                date: this.facade.formModel().date,
                value: this.facade.formModel().weight,
                unit: measurements.system() === 'metric' ? 'kg' : 'lb',
                valid: this.form().valid(),
                loading: this.isSaving(),
                editing: this.isEditing(),
                error: this.error(),
            }),
            dirty: () => this.form().dirty(),
            fill: (date, value) => {
                this.facade.formModel.set({ date, weight: value });
                this.form().markAsDirty();
            },
            clear: () => {
                this.form().reset({ date: this.facade.formModel().date, weight: '' });
            },
        });
        this.facade.entryError.set(null);
        effect(() => {
            this.dialogRef.disableClose = this.isSaving();
        });
        effect(() => {
            if (this.facade.entrySaveVersion() > this.initialSaveVersion) {
                this.dialogRef.close();
            }
        });
    }

    protected close(): void {
        if (this.isSaving()) {
            return;
        }
        if (this.isEditing()) {
            this.facade.cancelEdit();
        }
        this.dialogRef.close();
    }
}
