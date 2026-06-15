import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

@Component({
    selector: 'fd-user-manage-success-dialog',
    templateUrl: './update-success-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class UpdateSuccessDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<UpdateSuccessDialogComponent, boolean>);

    protected close(redirectHome: boolean): void {
        this.dialogRef.close(redirectHome);
    }
}
