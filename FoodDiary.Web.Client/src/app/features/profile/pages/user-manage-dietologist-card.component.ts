import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';

import type { DietologistPermissions, DietologistRelationship } from '../../dietologist/models/dietologist.data';
import type { DietologistFormData } from './user-manage.component';

export type DietologistPermissionChange = {
    controlName: keyof DietologistPermissions;
    value: boolean;
};

@Component({
    selector: 'fd-user-manage-dietologist-card',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiFormErrorComponent,
        FdUiInputComponent,
        FdUiSwitchComponent,
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

    public readonly dietologistInvite = output<void>();
    public readonly dietologistRevoke = output<void>();
    public readonly dietologistProfileToggle = output<boolean>();
    public readonly dietologistPermissionChange = output<DietologistPermissionChange>();
}
