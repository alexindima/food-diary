import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import type { ActivityLevelOption } from '../../../../../shared/models/user.data';
import type { UserFormValues } from '../../user-manage/user-manage-lib/user-manage.types';

@Component({
    selector: 'fd-user-manage-body-card',
    imports: [FormField, TranslatePipe, FdUiCardComponent, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './user-manage-body-card.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBodyCardComponent {
    public readonly userForm = input.required<FieldTree<UserFormValues>>();
    public readonly activityLevelOptions = input.required<Array<FdUiSelectOption<ActivityLevelOption | null>>>();
}
