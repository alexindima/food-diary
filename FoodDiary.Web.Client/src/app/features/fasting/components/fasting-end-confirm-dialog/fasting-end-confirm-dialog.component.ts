import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';

export type FastingEndConfirmDialogResult = 'confirm' | 'cancel';

export interface FastingEndConfirmDialogData {
    title: string;
    message: string;
    confirmLabel: string;
    cancelLabel: string;
}

@Component({
    selector: 'fd-fasting-end-confirm-dialog',
    standalone: true,
    imports: [FdUiDialogShellComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './fasting-end-confirm-dialog.component.html',
    styleUrl: './fasting-end-confirm-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingEndConfirmDialogComponent {
    public readonly data = inject<FastingEndConfirmDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<FastingEndConfirmDialogComponent, FastingEndConfirmDialogResult>>(FdUiDialogRef);

    public onConfirm(): void {
        this.dialogRef.close('confirm');
    }

    public onCancel(): void {
        this.dialogRef.close('cancel');
    }
}
