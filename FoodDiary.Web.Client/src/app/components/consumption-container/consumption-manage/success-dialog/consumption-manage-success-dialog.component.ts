import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

export interface ConsumptionManageSuccessDialogData {
    isEdit: boolean;
}

export type ConsumptionManageRedirectAction = 'Home' | 'ConsumptionList';

@Component({
    selector: 'fd-consumption-manage-success-dialog',
    standalone: true,
    template: `
        <fd-ui-dialog
            [title]="data.isEdit ? ('CONSUMPTION_MANAGE.EDIT_SUCCESS' | translate) : ('CONSUMPTION_MANAGE.CREATE_SUCCESS' | translate)"
            size="sm"
        >
            <div fdUiDialogFooter class="consumption-manage-success-dialog__footer">
                <fd-ui-button fill="text" (click)="close('Home')">
                    {{ 'CONSUMPTION_MANAGE.GO_TO_HOME_BUTTON' | translate }}
                </fd-ui-button>
                <fd-ui-button fill="text" (click)="close('ConsumptionList')">
                    {{ 'CONSUMPTION_MANAGE.GO_TO_CONSUMPTION_LIST_BUTTON' | translate }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styleUrls: ['./consumption-manage-success-dialog.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class ConsumptionManageSuccessDialogComponent {
    public constructor(
        @Inject(FD_UI_DIALOG_DATA) public readonly data: ConsumptionManageSuccessDialogData,
        private readonly dialogRef: FdUiDialogRef<ConsumptionManageSuccessDialogComponent, ConsumptionManageRedirectAction>,
    ) {}

    public close(action: ConsumptionManageRedirectAction): void {
        this.dialogRef.close(action);
    }
}
