import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogComponent } from './fd-ui-dialog.component';
import { FdUiButtonComponent } from '../button/fd-ui-button.component';

export interface FdUiConfirmDialogData {
    title?: string;
    message?: string;
    confirmLabel?: string;
    cancelLabel?: string;
    danger?: boolean;
}

@Component({
    selector: 'fd-ui-confirm-dialog',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiDialogComponent, FdUiButtonComponent],
    template: `
        <fd-ui-dialog [title]="data.title" size="sm" [dismissible]="false">
            <p class="fd-ui-confirm-dialog__message">{{ data.message }}</p>

            <div fdUiDialogFooter>
                <fd-ui-button variant="secondary" fill="text" size="sm" (click)="onCancel()">
                    {{ data.cancelLabel || ('COMMON.CANCEL' | translate) }}
                </fd-ui-button>
                <fd-ui-button
                    size="sm"
                    [variant]="data.danger ? 'danger' : 'primary'"
                    (click)="onConfirm()"
                >
                    {{ data.confirmLabel || ('COMMON.CONFIRM' | translate) }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styleUrls: ['./fd-ui-confirm-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiConfirmDialogComponent {
    public readonly data = inject(MAT_DIALOG_DATA) as FdUiConfirmDialogData;
    private readonly dialogRef = inject(MatDialogRef<FdUiConfirmDialogComponent>);

    public onConfirm(): void {
        this.dialogRef.close(true);
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}
