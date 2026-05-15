import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import type { ActivityLevelOption } from '../../../../../shared/models/user.data';
import type { UserFormData } from '../../user-manage/user-manage.types';

@Component({
    selector: 'fd-user-manage-body-card',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiCardComponent, FdUiInputComponent, FdUiSelectComponent],
    templateUrl: './user-manage-body-card.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBodyCardComponent {
    public readonly userForm = input.required<FormGroup<UserFormData>>();
    public readonly activityLevelOptions = input.required<Array<FdUiSelectOption<ActivityLevelOption | null>>>();
}
