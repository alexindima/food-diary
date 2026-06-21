import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import type { Gender } from '../../../../../shared/models/user.data';
import type { AppThemeName, AppUiStyleName } from '../../../../../theme/app-theme.config';
import type { PasswordActionState, ProfileStatusViewModel, UserFormValues } from '../../user-manage/user-manage-lib/user-manage.types';

const ISO_DATE_LENGTH = 10;

export type UserManageAccountFormPatch = Partial<
    Pick<UserFormValues, 'username' | 'firstName' | 'lastName' | 'birthDate' | 'gender' | 'language' | 'theme' | 'uiStyle' | 'profileImage'>
>;

@Component({
    selector: 'fd-user-manage-account-card',
    imports: [
        FormField,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiStatusBadgeComponent,
        ImageUploadFieldComponent,
    ],
    templateUrl: './user-manage-account-card.html',
    styleUrl: '../../user-manage/user-manage.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageAccountCardComponent {
    public readonly userForm = input.required<FieldTree<UserFormValues>>();
    public readonly profileStatus = input.required<ProfileStatusViewModel>();
    public readonly passwordActionState = input.required<PasswordActionState>();
    public readonly genderOptions = input.required<Array<FdUiSelectOption<Gender | null>>>();
    public readonly languageOptions = input.required<Array<FdUiSelectOption<string | null>>>();
    public readonly themeOptions = input.required<Array<FdUiSelectOption<AppThemeName | null>>>();
    public readonly uiStyleOptions = input.required<Array<FdUiSelectOption<AppUiStyleName | null>>>();

    public readonly passwordChange = output();
    public readonly userFormPatch = output<UserManageAccountFormPatch>();

    protected onTextFieldChange(field: 'username' | 'firstName' | 'lastName', value: string | number | null): void {
        const nextValue = value === null ? '' : String(value);
        this.emitFormPatch({ [field]: nextValue.length > 0 ? nextValue : null });
    }

    protected onBirthDateChange(value: string | Date | null): void {
        const nextValue = value instanceof Date ? value.toISOString().slice(0, ISO_DATE_LENGTH) : value;
        this.emitFormPatch({ birthDate: nextValue });
    }

    protected emitFormPatch(patch: UserManageAccountFormPatch): void {
        this.userFormPatch.emit(patch);
    }
}
