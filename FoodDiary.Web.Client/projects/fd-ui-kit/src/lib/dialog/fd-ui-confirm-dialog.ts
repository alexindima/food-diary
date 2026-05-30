import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiDialogComponent } from './fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from './fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

export type FdUiConfirmDialogData = {
    title?: string;
    message?: string;
    confirmLabel?: string;
    cancelLabel?: string;
    danger?: boolean;
};

@Component({
    selector: 'fd-ui-confirm-dialog',
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './fd-ui-confirm-dialog.html',
    styleUrls: ['./fd-ui-confirm-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiConfirmDialogComponent {
    protected readonly data = inject<FdUiConfirmDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject(FdUiDialogRef<FdUiConfirmDialogComponent>);

    protected onConfirm(): void {
        this.dialogRef.close(true);
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }
}
