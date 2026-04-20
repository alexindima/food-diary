import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

export type FastingSafetyDialogResult = 'confirm' | 'cancel' | 'close';

export interface FastingSafetyDialogData {
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    tone?: 'warning' | 'danger';
}

@Component({
    selector: 'fd-fasting-safety-dialog',
    standalone: true,
    imports: [FdUiDialogShellComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './fasting-safety-dialog.component.html',
    styleUrl: './fasting-safety-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingSafetyDialogComponent {
    public readonly data = inject<FastingSafetyDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<FastingSafetyDialogComponent, FastingSafetyDialogResult>>(FdUiDialogRef);

    public onConfirm(): void {
        this.dialogRef.close('confirm');
    }

    public onCancel(): void {
        this.dialogRef.close('cancel');
    }

    public onClose(): void {
        this.dialogRef.close('close');
    }
}
