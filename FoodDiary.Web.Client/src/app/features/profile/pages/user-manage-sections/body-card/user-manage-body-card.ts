import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import type { ActivityLevelOption } from '../../../../../shared/models/user.data';
import type { UserFormValues } from '../../user-manage/user-manage-lib/user-manage.types';

export type UserManageBodyFormPatch = Partial<Pick<UserFormValues, 'height' | 'activityLevel'>>;

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
    public readonly userFormPatch = output<UserManageBodyFormPatch>();

    protected onHeightChange(value: string | number | null): void {
        if (value === null || String(value).trim().length === 0) {
            this.emitFormPatch({ height: null });
            return;
        }

        const parsed = Number(value);
        this.emitFormPatch({ height: Number.isFinite(parsed) ? parsed : null });
    }

    protected emitFormPatch(patch: UserManageBodyFormPatch): void {
        this.userFormPatch.emit(patch);
    }
}
