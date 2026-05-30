import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

export type UnsavedChangesDialogResult = 'save' | 'discard' | 'stay';

export type UnsavedChangesDialogData = {
    title?: string;
    message?: string;
    saveLabel?: string;
    discardLabel?: string;
    stayLabel?: string;
};

@Component({
    selector: 'fd-unsaved-changes-dialog',
    imports: [TranslateModule, FdUiDialogShellComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './unsaved-changes-dialog.html',
    styleUrl: './unsaved-changes-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsavedChangesDialogComponent {
    protected readonly data = inject<UnsavedChangesDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });
    private readonly dialogRef = inject<FdUiDialogRef<UnsavedChangesDialogComponent, UnsavedChangesDialogResult>>(FdUiDialogRef);

    protected onSave(): void {
        this.dialogRef.close('save');
    }

    protected onDiscard(): void {
        this.dialogRef.close('discard');
    }

    protected onStay(): void {
        this.dialogRef.close('stay');
    }
}
