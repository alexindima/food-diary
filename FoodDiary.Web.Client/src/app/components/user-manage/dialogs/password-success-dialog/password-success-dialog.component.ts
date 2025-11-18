import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogComponent } from '../../../../ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from '../../../../ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from '../../../../ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-user-manage-password-success-dialog',
    standalone: true,
    template: `
        <fd-ui-dialog [title]="'USER_MANAGE.CHANGE_PASSWORD_SUCCESS' | translate" size="sm">
            <div fdUiDialogFooter class="password-success-dialog__footer">
                <fd-ui-button icon="check" (click)="close()">
                    {{ 'USER_MANAGE.CHANGE_PASSWORD_OK' | translate }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styleUrls: ['./password-success-dialog.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class PasswordSuccessDialogComponent {
    private readonly dialogRef = inject(MatDialogRef<PasswordSuccessDialogComponent>);

    public close(): void {
        this.dialogRef.close();
    }
}
