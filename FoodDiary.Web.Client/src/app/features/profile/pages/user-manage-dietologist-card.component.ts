import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';

import type { DietologistPermissions, DietologistRelationship } from '../../dietologist/models/dietologist.data';
import type { DietologistFormData } from './user-manage.component';
import { UserManageDietologistPermissionsComponent } from './user-manage-dietologist-permissions.component';
import { UserManageDietologistSummaryComponent } from './user-manage-dietologist-summary.component';

export type DietologistPermissionChange = {
    controlName: keyof DietologistPermissions;
    value: boolean;
};

@Component({
    selector: 'fd-user-manage-dietologist-card',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiFormErrorComponent,
        UserManageDietologistSummaryComponent,
        UserManageDietologistPermissionsComponent,
    ],
    templateUrl: './user-manage-dietologist-card.component.html',
    styleUrl: './user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageDietologistCardComponent {
    public readonly dietologistForm = input.required<FormGroup<DietologistFormData>>();
    public readonly dietologistRelationship = input.required<DietologistRelationship | null>();
    public readonly dietologistPermissions = input.required<DietologistPermissions>();
    public readonly dietologistError = input.required<string | null>();
    public readonly dietologistInviteEmailError = input.required<string | null>();
    public readonly dietologistAcceptedDateLabel = input.required<string | null>();
    public readonly dietologistExpiresDateLabel = input.required<string | null>();
    public readonly isLoadingDietologist = input.required<boolean>();
    public readonly isSavingDietologist = input.required<boolean>();
    public readonly hasDietologistRelationship = input.required<boolean>();
    public readonly isDietologistPending = input.required<boolean>();
    public readonly isDietologistConnected = input.required<boolean>();

    public readonly dietologistInvite = output();
    public readonly dietologistRevoke = output();
    public readonly dietologistProfileToggle = output<boolean>();
    public readonly dietologistPermissionChange = output<DietologistPermissionChange>();

    protected handleDietologistPermissionChange(change: DietologistPermissionChange): void {
        if (change.controlName === 'shareProfile') {
            this.dietologistProfileToggle.emit(change.value);
            return;
        }

        this.dietologistPermissionChange.emit(change);
    }
}
