import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { connectMeasurementDevelopment } from '../../../../shared/development/measurement-development';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WaistHistoryFormCardComponent } from '../../components/waist-history-form-card/waist-history-form-card';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';

@Component({
    selector: 'fd-waist-history-entry-dialog',
    imports: [TranslatePipe, FdUiDialogComponent, WaistHistoryFormCardComponent],
    templateUrl: './waist-history-entry-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryEntryDialogComponent {
    private readonly facade = inject(WaistHistoryFacade);
    private readonly dialogRef = inject(FdUiDialogRef<WaistHistoryEntryDialogComponent, void>);
    private readonly initialSaveVersion = this.facade.entrySaveVersion();

    protected readonly form = this.facade.form;
    protected readonly isSaving = this.facade.isSaving;
    protected readonly isEditing = this.facade.isEditing;
    protected readonly error = this.facade.entryError;

    public constructor() {
        const measurements = inject(MeasurementSystemService);
        connectMeasurementDevelopment({
            kind: 'waist',
            close: () => {
                this.close();
            },
            read: () => ({
                date: this.facade.formModel().date,
                value: this.facade.formModel().circumference,
                unit: measurements.system() === 'metric' ? 'cm' : 'in',
                valid: this.form().valid(),
                loading: this.isSaving(),
                editing: this.isEditing(),
                error: this.error(),
            }),
            dirty: () => this.form().dirty(),
            fill: (date, value) => {
                this.facade.formModel.set({ date, circumference: value });
                this.form().markAsDirty();
            },
            clear: () => {
                this.form().reset({ date: this.facade.formModel().date, circumference: '' });
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
