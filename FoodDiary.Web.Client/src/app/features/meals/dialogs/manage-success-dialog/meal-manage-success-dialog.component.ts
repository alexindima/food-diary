import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

export interface ConsumptionManageSuccessDialogData {
    isEdit: boolean;
}

export type ConsumptionManageRedirectAction = 'Home' | 'ConsumptionList';

@Component({
    selector: 'fd-meal-manage-success-dialog',
    standalone: true,
    templateUrl: './meal-manage-success-dialog.component.html',
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
