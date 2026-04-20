import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

export interface ConsumptionManageSuccessDialogData {
    isEdit: boolean;
}

export type ConsumptionManageRedirectAction = 'Home' | 'ConsumptionList';

@Component({
    selector: 'fd-meal-manage-success-dialog',
    standalone: true,
    template: `
        <fd-ui-dialog
            [title]="data.isEdit ? ('CONSUMPTION_MANAGE.EDIT_SUCCESS' | translate) : ('CONSUMPTION_MANAGE.CREATE_SUCCESS' | translate)"
            size="sm"
        >
            <div fdUiDialogFooter class="meal-manage-success-dialog__footer">
                <fd-ui-button fill="text" (click)="close('Home')">
                    {{ 'CONSUMPTION_MANAGE.GO_TO_HOME_BUTTON' | translate }}
                </fd-ui-button>
                <fd-ui-button fill="text" (click)="close('ConsumptionList')">
                    {{ 'CONSUMPTION_MANAGE.GO_TO_CONSUMPTION_LIST_BUTTON' | translate }}
                </fd-ui-button>
            </div>
        </fd-ui-dialog>
    `,
    styleUrls: ['./meal-manage-success-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class MealManageSuccessDialogComponent {
    public readonly data = inject<ConsumptionManageSuccessDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<MealManageSuccessDialogComponent, ConsumptionManageRedirectAction>>(FdUiDialogRef);

    public close(action: ConsumptionManageRedirectAction): void {
        this.dialogRef.close(action);
    }
}
