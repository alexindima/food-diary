import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, output, signal, viewChildren } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAutocompleteComponent, type FdUiAutocompleteOption } from 'fd-ui-kit/autocomplete/fd-ui-autocomplete';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInlineAlertComponent } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert';
import { fdUiCoerceInputTextValue, FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiMenuComponent } from 'fd-ui-kit/menu/fd-ui-menu';
import { FdUiMenuDividerComponent } from 'fd-ui-kit/menu/fd-ui-menu-divider';
import { FdUiMenuItemComponent } from 'fd-ui-kit/menu/fd-ui-menu-item';
import { FdUiMenuTriggerDirective } from 'fd-ui-kit/menu/fd-ui-menu-trigger.directive';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { ActivityLevelOption, Gender } from '../../../../../shared/models/user.data';
import type { AppThemeName, AppUiStyleName } from '../../../../../theme/app-theme.config';
import type { UserFormValues, UserManageAccountFormPatch, UserManageBodyFormPatch } from '../../../lib/user-manage.types';
import { calculateProfileCompleteness } from '../../user-manage/user-manage-lib/user-profile-completeness.mapper';

type HeightField = 'metric' | 'feet' | 'inches';

type ImperialHeightDraft = {
    canonicalCm: number | null;
    feet: number | null;
    inches: number | null;
};

@Component({
    selector: 'fd-user-manage-comparison-widgets',
    imports: [
        FormField,
        RouterLink,
        TranslatePipe,
        LocalizedNumberPipe,
        FdUiAutocompleteComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiIconComponent,
        FdUiInlineAlertComponent,
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
    protected readonly locale = injectCurrentLanguage();
    protected readonly latestBirthDate = new Date();
    protected readonly measurements = inject(MeasurementSystemService);
    protected readonly avatarClearRequest = signal(0);
    private readonly heightFields = viewChildren<FdUiInputComponent>('heightField');
    private readonly invalidHeightDrafts = signal<readonly HeightField[]>([]);
    public readonly userForm = input.required<FieldTree<UserFormValues>>();
    public readonly genderOptions = input.required<Array<FdUiSelectOption<Gender | null>>>();
    public readonly languageOptions = input.required<Array<FdUiSelectOption<string | null>>>();
    public readonly themeOptions = input.required<Array<FdUiSelectOption<AppThemeName | null>>>();
    public readonly uiStyleOptions = input.required<Array<FdUiSelectOption<AppUiStyleName | null>>>();
    public readonly activityLevelOptions = input.required<Array<FdUiSelectOption<ActivityLevelOption | null>>>();
    public readonly currentWeight = input.required<number | null>();
    public readonly currentWaist = input.required<number | null>();
    public readonly hasWeightLoadError = input(false);
    public readonly hasWaistLoadError = input(false);
    public readonly securityExpanded = input(false);

    public readonly userFormPatch = output<UserManageAccountFormPatch | UserManageBodyFormPatch>();
    public readonly birthDateEdited = output();
    public readonly birthDateInputInvalidChange = output<boolean>();
    public readonly heightInputInvalidChange = output<boolean>();
    public readonly weightRetry = output();
    public readonly waistRetry = output();
    protected readonly timeZoneQuery = signal<string | null>(null);
    protected readonly timeZoneValue = computed(() => this.timeZoneQuery() ?? this.userForm().timeZoneId().value());
    protected readonly timeZoneOptions = computed<Array<FdUiAutocompleteOption<string>>>(() => {
        const selected = this.userForm().timeZoneId().value();
        const available = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : [];
        const zones = new Set(['UTC', ...available]);
        if (selected !== null && selected.length > 0) {
            zones.add(selected);
        }
        return [...zones].sort().map(value => ({ label: value, value }));
    });
    protected readonly filteredTimeZoneOptions = computed(() => {
        const query = (this.timeZoneQuery() ?? '').trim().replaceAll('_', ' ').toLowerCase();
        return this.timeZoneOptions().filter(option => option.label.replaceAll('_', ' ').toLowerCase().includes(query));
    });

    protected resetTimeZoneSearch(): void {
        this.timeZoneQuery.set(null);
    }

    protected onTimeZoneTouch(): void {
        this.resetTimeZoneSearch();
        this.userForm().timeZoneId().markAsTouched();
    }

    protected onTimeZoneSelected(option: FdUiAutocompleteOption<string>): void {
        this.resetTimeZoneSearch();
        if (option.value !== this.userForm().timeZoneId().value()) {
            this.userFormPatch.emit({ timeZoneId: option.value });
        }
    }

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
    protected readonly imperialHeight = linkedSignal<number | null, ImperialHeightDraft>({
        source: () => this.userForm().heightCm().value(),
        computation: (heightCm, previous) => {
            if (previous?.value.canonicalCm === heightCm) {
                return previous.value;
            }

            const parts = heightCm === null ? { feet: null, inches: null } : this.measurements.displayHeight(heightCm);
            return { canonicalCm: heightCm, ...parts };
        },
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
        const nextValue = fdUiCoerceInputTextValue(value);
        this.userFormPatch.emit({ [field]: nextValue.length > 0 ? nextValue : null });
    }

    protected onHeightChange(value: string | number | null): void {
        this.userFormPatch.emit({ heightCm: this.heightPartValue(value) });
    }

    protected onMeasurementSystemChange(value: string | null): void {
        this.resetHeightInputDrafts();
        this.measurements.setSystem(value === 'imperial' ? 'imperial' : 'metric');
    }

    protected hasHeightError(field: HeightField): boolean {
        const invalidDrafts = this.invalidHeightDrafts();
        return invalidDrafts.length > 0 ? invalidDrafts.includes(field) : this.userForm().heightCm().invalid();
    }

    protected onHeightDraftInvalidChange(field: HeightField, invalid: boolean): void {
        this.invalidHeightDrafts.update(fields => (invalid ? [...fields, field] : fields.filter(value => value !== field)));
        this.heightInputInvalidChange.emit(this.invalidHeightDrafts().length > 0);
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- The profile page owns form discard.
    public resetHeightInputDrafts(): void {
        for (const field of this.heightFields()) {
            field.resetNativeNumberDraft();
        }
    }

    protected onImperialHeightChange(part: 'feet' | 'inches', value: string | number | null): void {
        const parsed = this.heightPartValue(value);

        const current = this.imperialHeight();
        const feet = part === 'feet' ? parsed : current.feet;
        const inches = part === 'inches' ? parsed : current.inches;
        const canonicalCm = feet === null && inches === null ? null : this.measurements.canonicalHeight(feet ?? 0, inches ?? 0);

        this.imperialHeight.set({ canonicalCm, feet, inches });
        this.userFormPatch.emit({ heightCm: canonicalCm });
    }

    private heightPartValue(value: string | number | null): number | null {
        return typeof value === 'number' ? value : null;
    }
}
