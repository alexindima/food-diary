import type { ImageSelection } from './image-upload.data';

export type ActivityLevelOption = 'MINIMAL' | 'LIGHT' | 'MODERATE' | 'HIGH' | 'EXTREME';
export type UiStyleOption = 'classic' | 'modern';

export interface DashboardLayoutSettings {
    web?: string[];
    mobile?: string[];
}

export interface User {
    id: string;
    email: string;
    hasPassword: boolean;
    username?: string;
    firstName?: string;
    lastName?: string;
    birthDate?: Date;
    gender?: string;
    weight?: number;
    desiredWeight?: number;
    desiredWaist?: number;
    height?: number;
    activityLevel?: ActivityLevelOption;
    dailyCalorieTarget?: number;
    proteinTarget?: number;
    fatTarget?: number;
    carbTarget?: number;
    fiberTarget?: number;
    stepGoal?: number;
    waterGoal?: number;
    hydrationGoal?: number;
    language?: string;
    theme?: string;
    uiStyle?: string;
    pushNotificationsEnabled: boolean;
    fastingPushNotificationsEnabled: boolean;
    socialPushNotificationsEnabled: boolean;
    fastingCheckInReminderHours: number;
    fastingCheckInFollowUpReminderHours: number;
    profileImage?: string;
    profileImageAssetId?: string;
    dashboardLayout?: DashboardLayoutSettings | null;
    isActive: boolean;
    isEmailConfirmed: boolean;
    lastLoginAtUtc?: string | null;
    aiConsentAcceptedAt?: string | null;
    calories?: number;
}

export interface UpdateUserFormValues {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    birthDate: Date | string | null;
    gender: string | null;
    language: string | null;
    height: number | null;
    activityLevel: ActivityLevelOption | null;
    stepGoal: number | null;
    hydrationGoal?: number | null;
    profileImage: ImageSelection | string | null;
    pushNotificationsEnabled?: boolean | null;
    fastingPushNotificationsEnabled?: boolean | null;
    socialPushNotificationsEnabled?: boolean | null;
    theme?: string | null;
    uiStyle?: UiStyleOption | null;
}

export class UpdateUserDto {
    public username?: string;
    public firstName?: string;
    public lastName?: string;
    public birthDate?: Date;
    public gender?: string;
    public height?: number;
    public activityLevel?: string;
    public stepGoal?: number;
    public hydrationGoal?: number;
    public language?: string;
    public theme?: string;
    public uiStyle?: string;
    public pushNotificationsEnabled?: boolean;
    public fastingPushNotificationsEnabled?: boolean;
    public socialPushNotificationsEnabled?: boolean;
    public profileImage?: string;
    public profileImageAssetId?: string;
    public dashboardLayout?: DashboardLayoutSettings | null;
    public isActive?: boolean;

    public constructor(formValues: Partial<UpdateUserFormValues>) {
        this.username = normalizeString(formValues.username);
        this.firstName = normalizeString(formValues.firstName);
        this.lastName = normalizeString(formValues.lastName);
        this.birthDate = normalizeDate(formValues.birthDate);
        this.gender = normalizeString(formValues.gender);
        this.height = normalizeNumber(formValues.height);
        this.activityLevel = normalizeActivityLevel(formValues.activityLevel);
        this.stepGoal = normalizeInteger(formValues.stepGoal);
        this.hydrationGoal = normalizeNumber((formValues as { hydrationGoal?: number | null }).hydrationGoal);
        this.language = normalizeLanguage((formValues as { language?: string | null }).language);
        this.theme = normalizeTheme((formValues as { theme?: string | null }).theme);
        this.uiStyle = normalizeUiStyle((formValues as { uiStyle?: UiStyleOption | null }).uiStyle);
        this.pushNotificationsEnabled = normalizeBoolean(
            (formValues as { pushNotificationsEnabled?: boolean | null }).pushNotificationsEnabled,
        );
        this.fastingPushNotificationsEnabled = normalizeBoolean(
            (formValues as { fastingPushNotificationsEnabled?: boolean | null }).fastingPushNotificationsEnabled,
        );
        this.socialPushNotificationsEnabled = normalizeBoolean(
            (formValues as { socialPushNotificationsEnabled?: boolean | null }).socialPushNotificationsEnabled,
        );
        const normalizedImage = normalizeProfileImage(formValues.profileImage);
        this.profileImage = normalizedImage?.url;
        this.profileImageAssetId = normalizedImage?.assetId;
    }
}

export class UpdateUserAppearanceDto {
    public theme?: string;
    public uiStyle?: string;

    public constructor(formValues: { theme?: string | null; uiStyle?: string | null }) {
        this.theme = normalizeTheme(formValues.theme);
        this.uiStyle = normalizeUiStyle(formValues.uiStyle);
    }
}

const normalizeString = (value: string | null | undefined): string | undefined => {
    const trimmed = value?.trim();
    return trimmed !== undefined && trimmed.length > 0 ? trimmed : undefined;
};

const normalizeDate = (value: Date | string | null | undefined): Date | undefined => {
    if (value === null || value === undefined || value === '') {
        return undefined;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return undefined;
    }

    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
};

const normalizeNumber = (value: number | null | undefined): number | undefined =>
    value === null || value === undefined || Number.isNaN(Number(value)) ? undefined : Number(value);

const normalizeInteger = (value: number | null | undefined): number | undefined => {
    const normalized = normalizeNumber(value);
    return normalized === undefined ? undefined : Math.round(normalized);
};

const normalizeActivityLevel = (value: ActivityLevelOption | null | undefined): string | undefined => {
    if (value === null || value === undefined) {
        return undefined;
    }

    const lower = value.toLowerCase();
    return lower.charAt(0).toUpperCase() + lower.slice(1);
};

const normalizeLanguage = (value: string | null | undefined): string | undefined => {
    if (value === null || value === undefined) {
        return undefined;
    }

    const normalized = value.trim().toLowerCase();
    return normalized.length > 0 ? normalized : undefined;
};

const normalizeTheme = (value: string | null | undefined): string | undefined => {
    if (value === null || value === undefined) {
        return undefined;
    }

    const normalized = value.trim().toLowerCase();
    return normalized.length > 0 ? normalized : undefined;
};

const normalizeUiStyle = (value: string | null | undefined): string | undefined => {
    if (value === null || value === undefined) {
        return undefined;
    }

    const normalized = value.trim().toLowerCase();
    return normalized.length > 0 ? normalized : undefined;
};

const normalizeBoolean = (value: boolean | null | undefined): boolean | undefined => value ?? undefined;

const normalizeProfileImage = (value: ImageSelection | string | null | undefined): { url: string; assetId?: string } | undefined => {
    if (value === null || value === undefined || value === '') {
        return undefined;
    }

    if (typeof value === 'string') {
        const normalized = normalizeString(value);
        return normalized !== undefined ? { url: normalized } : undefined;
    }

    const url = normalizeString(value.url ?? undefined);
    if (url === undefined) {
        return undefined;
    }

    const assetId = value.assetId ?? undefined;
    return { url, assetId: assetId ?? undefined };
};

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface SetPasswordRequest {
    newPassword: string;
}

export interface DesiredWeightResponse {
    desiredWeight: number | null;
}

export interface DesiredWaistResponse {
    desiredWaist: number | null;
}

export enum Gender {
    Male = 'M',
    Female = 'F',
    Other = 'O',
}
