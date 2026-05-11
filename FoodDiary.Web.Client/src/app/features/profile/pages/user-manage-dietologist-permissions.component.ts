import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch.component';

import type { DietologistPermissions } from '../../dietologist/models/dietologist.data';
import type { DietologistPermissionChange } from './user-manage-dietologist-card.component';

type DietologistPermissionOption = {
    controlName: keyof DietologistPermissions;
    labelKey: string;
};

const DIETOLOGIST_PERMISSION_OPTIONS: DietologistPermissionOption[] = [
    { controlName: 'shareProfile', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_PROFILE' },
    { controlName: 'shareMeals', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_MEALS' },
    { controlName: 'shareStatistics', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_STATISTICS' },
    { controlName: 'shareWeight', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_WEIGHT' },
    { controlName: 'shareWaist', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_WAIST' },
    { controlName: 'shareGoals', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_GOALS' },
    { controlName: 'shareHydration', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_HYDRATION' },
    { controlName: 'shareFasting', labelKey: 'USER_MANAGE.DIETOLOGIST_PERMISSION_FASTING' },
];

@Component({
    selector: 'fd-user-manage-dietologist-permissions',
    imports: [TranslatePipe, FdUiHintDirective, FdUiSwitchComponent],
    templateUrl: './user-manage-dietologist-permissions.component.html',
    styleUrl: './user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageDietologistPermissionsComponent {
    public readonly permissions = input.required<DietologistPermissions>();
    public readonly isSavingDietologist = input.required<boolean>();

    public readonly permissionChange = output<DietologistPermissionChange>();

    protected readonly permissionOptions = DIETOLOGIST_PERMISSION_OPTIONS;
}
