import { FormControl, FormGroup, Validators } from '@angular/forms';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { type ActivityLevelOption, Gender, type User } from '../../../../shared/models/user.data';
import {
    APP_THEMES,
    APP_UI_STYLES,
    type AppThemeName,
    type AppUiStyleName,
    isAppThemeName,
    isAppUiStyleName,
} from '../../../../theme/app-theme.config';
import { ACTIVITY_LEVEL_OPTIONS, DEFAULT_DIETOLOGIST_PERMISSIONS, LANGUAGE_CODES } from './user-manage.config';
import type { DietologistFormData, UserFormData, UserFormValues } from './user-manage.types';

const DATE_INPUT_PAD_LENGTH = 2;

export type UserManageSelectOptions = {
    genderOptions: Array<FdUiSelectOption<Gender | null>>;
    activityLevelOptions: Array<FdUiSelectOption<ActivityLevelOption | null>>;
    languageOptions: Array<FdUiSelectOption<string | null>>;
    themeOptions: Array<FdUiSelectOption<AppThemeName | null>>;
    uiStyleOptions: Array<FdUiSelectOption<AppUiStyleName | null>>;
};

export function createUserManageForm(): FormGroup<UserFormData> {
    return new FormGroup<UserFormData>({
        email: new FormControl<string | null>({ value: '', disabled: true }),
        username: new FormControl<string | null>(null),
        firstName: new FormControl<string | null>(null),
        lastName: new FormControl<string | null>(null),
        birthDate: new FormControl<string | null>(null),
        gender: new FormControl<Gender | null>(null),
        language: new FormControl<string | null>(null),
        theme: new FormControl<AppThemeName | null>(null),
        uiStyle: new FormControl<AppUiStyleName | null>(null),
        height: new FormControl<number | null>(null),
        activityLevel: new FormControl<ActivityLevelOption | null>(null),
        stepGoal: new FormControl<number | null>(null),
        profileImage: new FormControl<ImageSelection | null>(null),
    });
}

export function createDietologistForm(): FormGroup<DietologistFormData> {
    return new FormGroup<DietologistFormData>({
        email: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
        shareProfile: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareProfile, { nonNullable: true }),
        shareMeals: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareMeals, { nonNullable: true }),
        shareStatistics: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareStatistics, { nonNullable: true }),
        shareWeight: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareWeight, { nonNullable: true }),
        shareWaist: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareWaist, { nonNullable: true }),
        shareGoals: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareGoals, { nonNullable: true }),
        shareHydration: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareHydration, { nonNullable: true }),
        shareFasting: new FormControl<boolean>(DEFAULT_DIETOLOGIST_PERMISSIONS.shareFasting, { nonNullable: true }),
    });
}

export function buildUserManageSelectOptions(translate: (key: string) => string): UserManageSelectOptions {
    return {
        genderOptions: Object.values(Gender).map(gender => ({
            label: translate(`USER_MANAGE.GENDER_OPTIONS.${gender}`),
            value: gender,
        })),
        activityLevelOptions: ACTIVITY_LEVEL_OPTIONS.map(level => ({
            label: translate(`USER_MANAGE.ACTIVITY_LEVEL_OPTIONS.${level}`),
            value: level,
        })),
        languageOptions: LANGUAGE_CODES.map(code => ({
            label: translate(`USER_MANAGE.LANGUAGE_OPTIONS.${code.toUpperCase()}`),
            value: code,
        })),
        themeOptions: APP_THEMES.map(theme => ({
            label: translate(`USER_MANAGE.THEME_OPTIONS.${theme.name.toUpperCase()}`),
            value: theme.name,
        })),
        uiStyleOptions: APP_UI_STYLES.map(style => ({
            label: translate(`USER_MANAGE.UI_STYLE_OPTIONS.${style.name.toUpperCase()}`),
            value: style.name,
        })),
    };
}

export function mapUserToForm(user: User): Partial<UserFormValues> {
    return {
        email: user.email,
        username: user.username,
        firstName: toNullable(user.firstName),
        lastName: toNullable(user.lastName),
        gender: normalizeGender(user.gender),
        language: normalizeLanguage(user.language),
        theme: normalizeTheme(user.theme),
        uiStyle: normalizeUiStyle(user.uiStyle),
        birthDate: mapUserBirthDate(user.birthDate),
        height: toNullable(user.height),
        activityLevel: mapUserActivityLevel(user.activityLevel),
        stepGoal: toNullable(user.stepGoal),
        profileImage: mapUserProfileImage(user),
        pushNotificationsEnabled: user.pushNotificationsEnabled,
        fastingPushNotificationsEnabled: user.fastingPushNotificationsEnabled,
        socialPushNotificationsEnabled: user.socialPushNotificationsEnabled,
    };
}

function normalizeLanguage(value: string | null | undefined): string | null {
    if (value === null || value === undefined || value.length === 0) {
        return null;
    }

    const normalized = value.trim().toLowerCase();
    if (normalized.length === 0) {
        return null;
    }

    const [code] = normalized.split(/[-_]/);
    return code.length > 0 ? code : null;
}

function normalizeGender(value: string | null | undefined): Gender | null {
    if (value === null || value === undefined || value.length === 0) {
        return null;
    }

    const normalized = value.trim().toLowerCase();
    const genderMap: Record<string, Gender> = {
        m: Gender.Male,
        male: Gender.Male,
        f: Gender.Female,
        female: Gender.Female,
        o: Gender.Other,
        other: Gender.Other,
    };

    return genderMap[normalized] ?? null;
}

function normalizeTheme(value: string | null | undefined): AppThemeName | null {
    if (value === null || value === undefined || value.length === 0) {
        return null;
    }

    const normalized = value.trim().toLowerCase();
    const legacyThemeMap: Record<string, AppThemeName> = {
        default: 'ocean',
    };
    const resolved = legacyThemeMap[normalized] ?? normalized;

    return isAppThemeName(resolved) ? resolved : null;
}

function normalizeUiStyle(value: string | null | undefined): AppUiStyleName | null {
    if (value === null || value === undefined || value.length === 0) {
        return null;
    }

    const normalized = value.trim().toLowerCase();
    const legacyUiStyleMap: Record<string, AppUiStyleName> = {
        default: 'classic',
    };
    const resolved = legacyUiStyleMap[normalized] ?? normalized;

    return isAppUiStyleName(resolved) ? resolved : null;
}

function mapUserBirthDate(value: Date | string | null | undefined): string | null {
    return value !== null && value !== undefined ? formatDateInput(new Date(value)) : null;
}

function mapUserActivityLevel(value: string | null | undefined): ActivityLevelOption | null {
    const normalized = value?.toUpperCase();
    if (normalized === undefined || normalized.length === 0) {
        return null;
    }

    return isActivityLevelOption(normalized) ? normalized : null;
}

function isActivityLevelOption(value: string): value is ActivityLevelOption {
    return ACTIVITY_LEVEL_OPTIONS.some(option => option === value);
}

function mapUserProfileImage(user: User): ImageSelection | null {
    const profileImage = user.profileImage ?? '';
    return profileImage.length > 0 ? { url: profileImage, assetId: toNullable(user.profileImageAssetId) } : null;
}

function formatDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(DATE_INPUT_PAD_LENGTH, '0');
    const day = String(date.getDate()).padStart(DATE_INPUT_PAD_LENGTH, '0');
    return `${year}-${month}-${day}`;
}

function toNullable<T>(value: T | null | undefined): T | null {
    return value ?? null;
}
