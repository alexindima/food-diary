import { normalizeStartOfUtcDay, parseDateValue } from '../lib/local-date.utils';
import type { ImageSelectionFields } from './image-upload.data';
import type { CalendarDate, UtcInstant } from './semantics/date-value';
import type { ImageAssetId, UserId, WaistGoalId, WeightGoalId } from './semantics/entity-id';

export type ActivityLevelOption = 'MINIMAL' | 'LIGHT' | 'MODERATE' | 'HIGH' | 'EXTREME';
export type UiStyleOption = 'classic' | 'modern';

export type DashboardLayoutSettings = {
    web?: string[];
    mobile?: string[];
};

export type User = {
    id: UserId;
    email: string | null;
    hasPassword: boolean;
    hasGoogleIdentity?: boolean;
    hasTelegramIdentity?: boolean;
    timeZoneId?: string | null;
    mustChangePassword?: boolean;
    username?: string;
    firstName?: string;
    lastName?: string;
    birthDate?: CalendarDate;
    gender?: string;
    weightKg?: number;
    desiredWeightKg?: number;
    desiredWaistCm?: number;
    heightCm?: number;
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
    surfaceStyle?: string;
    pushNotificationsEnabled: boolean;
    fastingPushNotificationsEnabled: boolean;
    socialPushNotificationsEnabled: boolean;
    fastingCheckInReminderHours: number;
    fastingCheckInFollowUpReminderHours: number;
    profileImage?: string;
    profileImageAssetId?: ImageAssetId;
    dashboardLayout?: DashboardLayoutSettings | null;
    isActive: boolean;
    isEmailConfirmed: boolean;
    lastLoginAtUtc?: UtcInstant | null;
    aiConsentAcceptedAt?: UtcInstant | null;
    calories?: number;
};

export type UpdateUserFormValues = {
    timeZoneId?: string | null;
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    birthDate: Date | string | null;
    gender: string | null;
    language: string | null;
    heightCm: number | null;
    activityLevel: ActivityLevelOption | null;
    stepGoal: number | null;
    hydrationGoal?: number | null;
    profileImage: ImageSelectionFields | string | null;
    pushNotificationsEnabled?: boolean | null;
    fastingPushNotificationsEnabled?: boolean | null;
    socialPushNotificationsEnabled?: boolean | null;
    theme?: string | null;
    uiStyle?: UiStyleOption | null;
};

export class UpdateUserDto {
    public timeZoneId?: string;
    public username?: string;
    public firstName?: string;
    public lastName?: string;
    public birthDate?: Date | null;
    public gender?: string;
    public heightCm?: number;
    public activityLevel?: string;
    public stepGoal?: number;
    public hydrationGoal?: number;
    public language?: string;
    public theme?: string;
    public uiStyle?: string;
    public pushNotificationsEnabled?: boolean;
    public fastingPushNotificationsEnabled?: boolean;
    public socialPushNotificationsEnabled?: boolean;
    public profileImage?: string | null;
    public profileImageAssetId?: string | null;
    public dashboardLayout?: DashboardLayoutSettings | null;
    public isActive?: boolean;

    public constructor(formValues: Partial<UpdateUserFormValues>) {
        this.timeZoneId = normalizeString(formValues.timeZoneId);
        this.username = normalizeProfileName(formValues.username);
        this.firstName = normalizeProfileName(formValues.firstName);
        this.lastName = normalizeProfileName(formValues.lastName);
        this.birthDate = normalizeDate(formValues.birthDate);
        this.gender = normalizeString(formValues.gender);
        this.heightCm = normalizeNumber(formValues.heightCm);
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
        this.profileImage = normalizedImage === null ? null : normalizedImage?.url;
        this.profileImageAssetId = normalizedImage === null ? null : normalizedImage?.assetId;
    }
}

export class UpdateUserAppearanceDto {
    public theme?: string;
    public uiStyle?: string;
    public surfaceStyle?: string;

    public constructor(formValues: { theme?: string | null; uiStyle?: string | null; surfaceStyle?: string | null }) {
        this.theme = normalizeTheme(formValues.theme);
        this.uiStyle = normalizeUiStyle(formValues.uiStyle);
        this.surfaceStyle = formValues.surfaceStyle ?? undefined;
    }
}

const normalizeProfileName = (value: string | null | undefined): string | undefined =>
    value === undefined ? undefined : (value ?? '').trim();

const normalizeString = (value: string | null | undefined): string | undefined => {
    const trimmed = value?.trim();
    return trimmed !== undefined && trimmed.length > 0 ? trimmed : undefined;
};

const normalizeDate = (value: Date | string | null | undefined): Date | null | undefined => {
    if (value === null) {
        return null;
    }
    const date = parseDateValue(value);
    return date !== null ? normalizeStartOfUtcDay(date) : undefined;
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

const normalizeLowercaseString = (value: string | null | undefined): string | undefined => {
    if (value === null || value === undefined) {
        return undefined;
    }

    const normalized = value.trim().toLowerCase();
    return normalized.length > 0 ? normalized : undefined;
};

const normalizeLanguage = normalizeLowercaseString;
const normalizeTheme = normalizeLowercaseString;
const normalizeUiStyle = normalizeLowercaseString;

const normalizeBoolean = (value: boolean | null | undefined): boolean | undefined => value ?? undefined;

const normalizeProfileImage = (
    value: ImageSelectionFields | string | null | undefined,
): { url: string; assetId?: string } | null | undefined => {
    if (value === undefined) {
        return undefined;
    }

    if (value === null || value === '') {
        return null;
    }

    if (typeof value === 'string') {
        const normalized = normalizeString(value);
        return normalized !== undefined ? { url: normalized } : null;
    }

    const url = normalizeString(value.url ?? undefined);
    if (url === undefined) {
        return null;
    }

    const assetId = value.assetId ?? undefined;
    return { url, assetId: assetId ?? undefined };
};

export type ChangePasswordRequest = {
    currentPassword: string;
    newPassword: string;
};

export type SetPasswordRequest = {
    newPassword: string;
};

export type DesiredWeightResponse = {
    desiredWeightKg: number | null;
    startWeightKg: number | null;
    startedAtUtc: UtcInstant | null;
};

export type WeightGoalHistoryItem = {
    id: WeightGoalId;
    targetWeightKg: number;
    startWeightKg: number;
    endWeightKg: number | null;
    startedAtUtc: UtcInstant;
    endedAtUtc: UtcInstant | null;
    status: 'Active' | 'Replaced' | 'Cancelled';
};

export type DesiredWaistResponse = {
    desiredWaistCm: number | null;
    startWaistCm: number | null;
    startedAtUtc: UtcInstant | null;
};

export type WaistGoalHistoryItem = {
    id: WaistGoalId;
    targetWaistCm: number;
    startWaistCm: number;
    endWaistCm: number | null;
    startedAtUtc: UtcInstant;
    endedAtUtc: UtcInstant | null;
    status: 'Active' | 'Replaced' | 'Cancelled';
};

export enum Gender {
    Male = 'M',
    Female = 'F',
    Other = 'O',
}

export type GoalHistoryPage<T> = {
    items: T[];
    nextCursor: string | null;
};
