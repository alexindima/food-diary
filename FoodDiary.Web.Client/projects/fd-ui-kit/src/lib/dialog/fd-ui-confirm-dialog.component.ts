import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { FdUiButtonComponent } from '../button/fd-ui-button.component';
import { FdUiDialogComponent } from './fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from './fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from './fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

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
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './fd-ui-confirm-dialog.component.html',
    styleUrls: ['./fd-ui-confirm-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiConfirmDialogComponent {
    public readonly data = inject(FD_UI_DIALOG_DATA) as FdUiConfirmDialogData;
    private readonly dialogRef = inject(FdUiDialogRef<FdUiConfirmDialogComponent>);

    public onConfirm(): void {
        this.dialogRef.close(true);
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}
