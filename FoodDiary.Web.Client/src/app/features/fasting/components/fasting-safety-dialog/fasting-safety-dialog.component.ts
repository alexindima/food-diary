import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';

export type FastingSafetyDialogResult = 'confirm' | 'cancel' | 'close';

export type FastingSafetyDialogData = {
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    tone?: 'warning' | 'danger';
};

@Component({
    selector: 'fd-fasting-safety-dialog',
    imports: [FdUiDialogShellComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './fasting-safety-dialog.component.html',
    styleUrl: './fasting-safety-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingSafetyDialogComponent {
    protected readonly data = inject<FastingSafetyDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<FastingSafetyDialogComponent, FastingSafetyDialogResult>>(FdUiDialogRef);

    protected onConfirm(): void {
        this.dialogRef.close('confirm');
    }

    protected onCancel(): void {
        this.dialogRef.close('cancel');
    }

    protected onClose(): void {
        this.dialogRef.close('close');
    }
}
