import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

export type ConsumptionManageSuccessDialogData = {
    isEdit: boolean;
};

export type ConsumptionManageRedirectAction = 'Home' | 'ConsumptionList';

@Component({
    selector: 'fd-meal-manage-success-dialog',
    templateUrl: './meal-manage-success-dialog.html',
    styleUrls: ['./meal-manage-success-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
})
export class MealManageSuccessDialogComponent {
    protected readonly data = inject<ConsumptionManageSuccessDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<MealManageSuccessDialogComponent, ConsumptionManageRedirectAction>>(FdUiDialogRef);
    protected readonly titleKey = this.data.isEdit ? 'CONSUMPTION_MANAGE.EDIT_SUCCESS' : 'CONSUMPTION_MANAGE.CREATE_SUCCESS';

    protected close(action: ConsumptionManageRedirectAction): void {
        this.dialogRef.close(action);
    }
}
