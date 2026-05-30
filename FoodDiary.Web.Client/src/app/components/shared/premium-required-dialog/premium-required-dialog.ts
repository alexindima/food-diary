import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

export type PremiumRequiredDialogData = {
    title?: string;
    message?: string;
    note?: string;
    actionLabel?: string;
    cancelLabel?: string;
};

@Component({
    selector: 'fd-premium-required-dialog',
    templateUrl: './premium-required-dialog.html',
    styleUrls: ['./premium-required-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class PremiumRequiredDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<PremiumRequiredDialogComponent, boolean>);
    protected readonly data = inject<PremiumRequiredDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};

    protected onConfirm(): void {
        this.dialogRef.close(true);
    }

    protected onCancel(): void {
        this.dialogRef.close(false);
    }
}
