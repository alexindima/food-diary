import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge.component';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import type { Gender } from '../../../../../shared/models/user.data';
import type { AppThemeName, AppUiStyleName } from '../../../../../theme/app-theme.config';
import type { PasswordActionState, ProfileStatusViewModel, UserFormData } from '../../user-manage/user-manage-lib/user-manage.types';

@Component({
    selector: 'fd-user-manage-account-card',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiStatusBadgeComponent,
        ImageUploadFieldComponent,
    ],
    templateUrl: './user-manage-account-card.component.html',
    styleUrl: '../../user-manage/user-manage.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageAccountCardComponent {
    public readonly userForm = input.required<FormGroup<UserFormData>>();
    public readonly profileStatus = input.required<ProfileStatusViewModel>();
    public readonly passwordActionState = input.required<PasswordActionState>();
    public readonly genderOptions = input.required<Array<FdUiSelectOption<Gender | null>>>();
    public readonly languageOptions = input.required<Array<FdUiSelectOption<string | null>>>();
    public readonly themeOptions = input.required<Array<FdUiSelectOption<AppThemeName | null>>>();
    public readonly uiStyleOptions = input.required<Array<FdUiSelectOption<AppUiStyleName | null>>>();

    public readonly passwordChange = output();
}
