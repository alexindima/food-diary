import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiMenuComponent } from 'fd-ui-kit/menu/fd-ui-menu';
import { FdUiMenuDividerComponent } from 'fd-ui-kit/menu/fd-ui-menu-divider';
import { FdUiMenuItemComponent } from 'fd-ui-kit/menu/fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from 'fd-ui-kit/menu/fd-ui-menu-trigger.directive';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { ActivityLevelOption, Gender } from '../../../../../shared/models/user.data';
import type { AppThemeName, AppUiStyleName } from '../../../../../theme/app-theme.config';
import type {
    UserFormValues,
    UserManageAccountFormPatch,
    UserManageBodyFormPatch,
} from '../../user-manage/user-manage-lib/user-manage.types';
import { calculateProfileCompleteness } from '../../user-manage/user-manage-lib/user-profile-completeness.mapper';

const ISO_DATE_LENGTH = 10;
const MIN_HEIGHT_FEET = 1;
const MAX_HEIGHT_FEET = 8;
const MIN_HEIGHT_INCHES = 0;
const MAX_HEIGHT_INCHES = 11;
const IMPERIAL_HEIGHT_RANGES = {
    feet: { min: MIN_HEIGHT_FEET, max: MAX_HEIGHT_FEET },
    inches: { min: MIN_HEIGHT_INCHES, max: MAX_HEIGHT_INCHES },
} as const;

@Component({
    selector: 'fd-user-manage-comparison-widgets',
    imports: [
        FormField,
        RouterLink,
        TranslatePipe,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiIconComponent,
        FdUiInputComponent,
        FdUiMenuComponent,
        FdUiMenuDividerComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
        FdUiSelectComponent,
        ImageUploadFieldComponent,
    ],
    templateUrl: './user-manage-comparison-widgets.html',
    styleUrl: './user-manage-comparison-widgets.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComparisonWidgetsComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    protected readonly avatarClearRequest = signal(0);
    public readonly userForm = input.required<FieldTree<UserFormValues>>();
    public readonly genderOptions = input.required<Array<FdUiSelectOption<Gender | null>>>();
    public readonly languageOptions = input.required<Array<FdUiSelectOption<string | null>>>();
    public readonly themeOptions = input.required<Array<FdUiSelectOption<AppThemeName | null>>>();
    public readonly uiStyleOptions = input.required<Array<FdUiSelectOption<AppUiStyleName | null>>>();
    public readonly activityLevelOptions = input.required<Array<FdUiSelectOption<ActivityLevelOption | null>>>();
    public readonly currentWeight = input.required<number | null>();
    public readonly currentWaist = input.required<number | null>();

    public readonly userFormPatch = output<UserManageAccountFormPatch | UserManageBodyFormPatch>();

    protected readonly measurementSystem = this.measurements.system;

    protected requestAvatarClear(): void {
        this.avatarClearRequest.update(request => request + 1);
    }
    protected readonly displayedWeight = computed(() => {
        const weightKg = this.currentWeight();
        return weightKg === null ? null : this.measurements.displayWeight(weightKg);
    });
    protected readonly displayedWaist = computed(() => {
        const waistCm = this.currentWaist();
        return waistCm === null ? null : this.measurements.displayLength(waistCm);
    });
    protected readonly imperialHeight = computed(() => {
        const heightCm = this.userForm().heightCm().value();
        return heightCm === null ? { feet: null, inches: null } : this.measurements.displayHeight(heightCm);
    });

    protected readonly completeness = computed(() => {
        const form = this.userForm();
        return calculateProfileCompleteness({
            birthDate: form.birthDate().value(),
            gender: form.gender().value(),
            heightCm: form.heightCm().value(),
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
            this.userFormPatch.emit({ heightCm: null });
            return;
        }

        const parsed = Number(value);
        this.userFormPatch.emit({ heightCm: Number.isFinite(parsed) ? parsed : null });
    }

    protected onMeasurementSystemChange(value: string | null): void {
        this.measurements.setSystem(value === 'imperial' ? 'imperial' : 'metric');
    }

    protected onImperialHeightChange(part: 'feet' | 'inches', value: string | number | null): void {
        const parsed = this.parseImperialHeightPart(part, value);
        if (parsed === null) {
            this.userFormPatch.emit({ heightCm: null });
            return;
        }
        if (parsed === undefined) {
            return;
        }

        const current = this.imperialHeight();
        const feet = part === 'feet' ? parsed : (current.feet ?? 0);
        const inches = part === 'inches' ? parsed : (current.inches ?? 0);
        this.userFormPatch.emit({ heightCm: this.measurements.canonicalHeight(feet, inches) });
    }

    private parseImperialHeightPart(part: 'feet' | 'inches', value: string | number | null): number | null | undefined {
        if (value === null || String(value).trim().length === 0) {
            return null;
        }

        const parsed = Number(value);
        const range = IMPERIAL_HEIGHT_RANGES[part];
        return Number.isFinite(parsed) && parsed >= range.min && parsed <= range.max ? parsed : undefined;
    }
}
