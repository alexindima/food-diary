import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';

import type { DietologistPermissions, DietologistRelationship } from '../../../../../shared/models/dietologist.data';
import type { DietologistFormValues, DietologistPermissionChange } from '../../user-manage/user-manage-lib/user-manage.types';
import { UserManageDietologistPermissionsComponent } from '../dietologist-permissions/user-manage-dietologist-permissions';
import { UserManageDietologistSummaryComponent } from '../dietologist-summary/user-manage-dietologist-summary';

@Component({
    selector: 'fd-user-manage-dietologist-card',
    imports: [
        TranslatePipe,
        FdUiCardComponent,
        FdUiFormErrorComponent,
        UserManageDietologistSummaryComponent,
        UserManageDietologistPermissionsComponent,
    ],
    templateUrl: './user-manage-dietologist-card.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageDietologistCardComponent {
    public readonly dietologistForm = input.required<FieldTree<DietologistFormValues>>();
    public readonly dietologistRelationship = input.required<DietologistRelationship | null>();
    public readonly dietologistPermissions = input.required<DietologistPermissions>();
    public readonly dietologistError = input.required<string | null>();
    public readonly dietologistInviteEmailError = input.required<string | null>();
    public readonly isLoadingDietologist = input.required<boolean>();
    public readonly isSavingDietologist = input.required<boolean>();

    public readonly dietologistInvite = output();
    public readonly dietologistRevoke = output();
    public readonly dietologistProfileToggle = output<boolean>();
    public readonly dietologistPermissionChange = output<DietologistPermissionChange>();

    protected changeDietologistPermission(change: DietologistPermissionChange): void {
        if (change.controlName === 'shareProfile') {
            this.dietologistProfileToggle.emit(change.value);
            return;
        }

        this.dietologistPermissionChange.emit(change);
    }
}
