import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-user-manage-success-dialog',
    standalone: true,
    template: `
        <fd-ui-dialog [title]="'USER_MANAGE.UPDATE_SUCCESS' | translate" size="sm">
            <div fdUiDialogFooter class="user-manage-success-dialog__footer">
                <fd-ui-button icon="home" (click)="close(true)">
                    {{ 'USER_MANAGE.GO_TO_HOME_BUTTON' | translate }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styleUrls: ['./update-success-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class UpdateSuccessDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<UpdateSuccessDialogComponent, boolean>);

    public close(redirectHome: boolean): void {
        this.dialogRef.close(redirectHome);
    }
}
