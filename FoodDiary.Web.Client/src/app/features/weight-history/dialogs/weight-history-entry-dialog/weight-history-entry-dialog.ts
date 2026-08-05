import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

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
        effect(() => {
            if (this.facade.entrySaveVersion() > this.initialSaveVersion) {
                this.dialogRef.close();
            }
        });
    }

    protected close(): void {
        if (this.isEditing()) {
            this.facade.cancelEdit();
        }
        this.dialogRef.close();
    }
}
