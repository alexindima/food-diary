import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';

export type UnsavedChangesDialogResult = 'save' | 'discard' | 'stay';

export interface UnsavedChangesDialogData {
    title?: string;
    message?: string;
    saveLabel?: string;
    discardLabel?: string;
    stayLabel?: string;
}

@Component({
    selector: 'fd-unsaved-changes-dialog',
    standalone: true,
    imports: [TranslateModule, FdUiDialogShellComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './unsaved-changes-dialog.component.html',
    styleUrl: './unsaved-changes-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsavedChangesDialogComponent {
    public readonly data = inject<UnsavedChangesDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });
    private readonly dialogRef = inject<FdUiDialogRef<UnsavedChangesDialogComponent, UnsavedChangesDialogResult>>(
        FdUiDialogRef,
    );

    public onSave(): void {
        this.dialogRef.close('save');
    }

    public onDiscard(): void {
        this.dialogRef.close('discard');
    }

    public onStay(): void {
        this.dialogRef.close('stay');
    }
}
