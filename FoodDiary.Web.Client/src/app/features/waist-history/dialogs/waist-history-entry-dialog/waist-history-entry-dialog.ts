import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

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
