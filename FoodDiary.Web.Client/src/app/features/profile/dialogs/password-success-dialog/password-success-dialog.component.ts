import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

@Component({
    selector: 'fd-user-manage-password-success-dialog',
    standalone: true,
    templateUrl: './password-success-dialog.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class PasswordSuccessDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<PasswordSuccessDialogComponent>);

    public close(): void {
        this.dialogRef.close();
    }
}
