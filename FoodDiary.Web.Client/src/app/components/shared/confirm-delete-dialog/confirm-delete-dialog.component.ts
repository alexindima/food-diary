import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslatePipe } from '@ngx-translate/core';

export interface ConfirmDeleteDialogData {
    title?: string;
    message?: string;
    name?: string | null;
    entityType?: string | null;
    confirmLabel?: string;
    cancelLabel?: string;
}

@Component({
    selector: 'fd-confirm-delete-dialog',
    standalone: true,
    templateUrl: './confirm-delete-dialog.component.html',
    styleUrls: ['./confirm-delete-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class ConfirmDeleteDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<ConfirmDeleteDialogComponent, boolean>);
    public readonly data = inject<ConfirmDeleteDialogData>(FD_UI_DIALOG_DATA);

    public onConfirm(): void {
        this.dialogRef.close(true);
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}
