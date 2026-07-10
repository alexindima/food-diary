import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

export type ConfirmDeleteDialogData = {
    title?: string;
    message?: string;
    name?: string | null;
    entityType?: string | null;
    confirmLabel?: string;
    cancelLabel?: string;
    confirmIcon?: string;
};

@Component({
    selector: 'fd-confirm-delete-dialog',
    templateUrl: './confirm-delete-dialog.html',
    styleUrls: ['./confirm-delete-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class ConfirmDeleteDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ConfirmDeleteDialogComponent, boolean>);
    protected readonly data = inject<ConfirmDeleteDialogData>(FD_UI_DIALOG_DATA);

    protected onConfirm(): void {
        this.dialogRef.close(true);
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }
}
