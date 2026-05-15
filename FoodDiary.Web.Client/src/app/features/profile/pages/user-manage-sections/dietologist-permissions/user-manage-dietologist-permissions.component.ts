import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';

import type { DietologistPermissions } from '../../../../../shared/models/dietologist.data';
import { DIETOLOGIST_PERMISSION_OPTIONS } from '../../user-manage/user-manage.config';
import type { DietologistPermissionChange } from '../../user-manage/user-manage.types';

@Component({
    selector: 'fd-user-manage-dietologist-permissions',
    imports: [TranslatePipe, FdUiHintDirective, FdUiSwitchComponent],
    templateUrl: './user-manage-dietologist-permissions.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageDietologistPermissionsComponent {
    public readonly permissions = input.required<DietologistPermissions>();
    public readonly isSavingDietologist = input.required<boolean>();

    public readonly permissionChange = output<DietologistPermissionChange>();

    protected readonly permissionOptions = DIETOLOGIST_PERMISSION_OPTIONS;
}
