import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
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
    templateUrl: './premium-required-dialog.component.html',
    styleUrls: ['./premium-required-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, TranslatePipe],
})
export class PremiumRequiredDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<PremiumRequiredDialogComponent, boolean>);
    public readonly data = inject<PremiumRequiredDialogData>(FD_UI_DIALOG_DATA, { optional: true }) ?? {};

    public onConfirm(): void {
        this.dialogRef.close(true);
    }

    public onCancel(): void {
        this.dialogRef.close(false);
    }
}
