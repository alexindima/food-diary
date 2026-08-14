import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiStatusBadgeComponent } from 'fd-ui-kit/status-badge/fd-ui-status-badge';

import type { ActivityLevelOption, Gender } from '../../../../../shared/models/user.data';
import type { AppThemeName, AppUiStyleName } from '../../../../../theme/app-theme.config';
import type { PasswordActionState, ProfileStatusViewModel, UserFormValues } from '../../user-manage/user-manage-lib/user-manage.types';
import { calculateProfileCompleteness } from '../../user-manage/user-manage-lib/user-profile-completeness.mapper';
import type { UserManageAccountFormPatch } from '../account-card/user-manage-account-card';
import type { UserManageBodyFormPatch } from '../body-card/user-manage-body-card';

const ISO_DATE_LENGTH = 10;
type MeasurementSystem = 'metric' | 'imperial';

@Component({
    selector: 'fd-user-manage-comparison-widgets',
    imports: [
        FormField,
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiIconComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiStatusBadgeComponent,
    ],
    templateUrl: './user-manage-comparison-widgets.html',
    styleUrl: './user-manage-comparison-widgets.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComparisonWidgetsComponent {
    public readonly userForm = input.required<FieldTree<UserFormValues>>();
    public readonly profileStatus = input.required<ProfileStatusViewModel>();
    public readonly passwordActionState = input.required<PasswordActionState>();
    public readonly genderOptions = input.required<Array<FdUiSelectOption<Gender | null>>>();
    public readonly languageOptions = input.required<Array<FdUiSelectOption<string | null>>>();
    public readonly themeOptions = input.required<Array<FdUiSelectOption<AppThemeName | null>>>();
    public readonly uiStyleOptions = input.required<Array<FdUiSelectOption<AppUiStyleName | null>>>();
    public readonly activityLevelOptions = input.required<Array<FdUiSelectOption<ActivityLevelOption | null>>>();
    public readonly currentWeight = input.required<number | null>();
    public readonly currentWaist = input.required<number | null>();

    public readonly passwordChange = output();
    public readonly saveNow = output();
    public readonly userFormPatch = output<UserManageAccountFormPatch | UserManageBodyFormPatch>();

    protected readonly measurementSystem = signal<MeasurementSystem>('metric');

    protected readonly completeness = computed(() => {
        const form = this.userForm();
        return calculateProfileCompleteness({
            birthDate: form.birthDate().value(),
            gender: form.gender().value(),
            height: form.height().value(),
            activityLevel: form.activityLevel().value(),
        });
    });

    protected readonly profileImageUrl = computed(() => this.userForm().profileImage().value()?.url ?? null);
    protected readonly identityTitle = computed(() => {
        const form = this.userForm();
        const fullName = [form.firstName().value(), form.lastName().value()].filter(Boolean).join(' ').trim();
        return fullName.length > 0 ? fullName : (form.email().value() ?? '');
    });

    protected onTextFieldChange(field: 'username' | 'firstName' | 'lastName', value: string | number | null): void {
        const nextValue = value === null ? '' : String(value);
        this.userFormPatch.emit({ [field]: nextValue.length > 0 ? nextValue : null });
    }

    protected onBirthDateChange(value: string | Date | null): void {
        const nextValue = value instanceof Date ? value.toISOString().slice(0, ISO_DATE_LENGTH) : value;
        this.userFormPatch.emit({ birthDate: nextValue });
    }

    protected onHeightChange(value: string | number | null): void {
        if (value === null || String(value).trim().length === 0) {
            this.userFormPatch.emit({ height: null });
            return;
        }

        const parsed = Number(value);
        this.userFormPatch.emit({ height: Number.isFinite(parsed) ? parsed : null });
    }

    protected onMeasurementSystemChange(value: string | null): void {
        this.measurementSystem.set(value === 'imperial' ? 'imperial' : 'metric');
    }
}
