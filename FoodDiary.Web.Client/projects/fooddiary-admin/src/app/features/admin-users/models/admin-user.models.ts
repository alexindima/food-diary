import type { AdminCalendarDate, AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminUser = {
    id: AdminId<'user'>;
    email: string | null;
    hasPassword?: boolean;
    mustChangePassword?: boolean;
    username?: string | null;
    firstName?: string | null;
    lastName?: string | null;
    birthDate?: AdminCalendarDate | null;
    gender?: string | null;
    weightKg?: number | null;
    latestWeightKg?: number | null;
    latestWeightDate?: AdminCalendarDate | null;
    desiredWeightKg?: number | null;
    desiredWaistCm?: number | null;
    heightCm?: number | null;
    activityLevel?: string | null;
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    stepGoal?: number | null;
    waterGoal?: number | null;
    hydrationGoal?: number | null;
    calorieCyclingEnabled?: boolean;
    mondayCalories?: number | null;
    tuesdayCalories?: number | null;
    wednesdayCalories?: number | null;
    thursdayCalories?: number | null;
    fridayCalories?: number | null;
    saturdayCalories?: number | null;
    sundayCalories?: number | null;
    profileImage?: string | null;
    profileImageAssetId?: AdminId<'image-asset'> | null;
    dashboardLayoutJson?: string | null;
    language?: string | null;
    theme?: string | null;
    uiStyle?: string | null;
    pushNotificationsEnabled?: boolean;
    fastingPushNotificationsEnabled?: boolean;
    socialPushNotificationsEnabled?: boolean;
    fastingCheckInReminderHours?: number;
    fastingCheckInFollowUpReminderHours?: number;
    telegramUserId?: number | null;
    isActive: boolean;
    isEmailConfirmed: boolean;
    createdOnUtc: AdminUtcInstant;
    deletedAt?: AdminUtcInstant | null;
    lastLoginAtUtc?: AdminUtcInstant | null;
    roles: string[];
    aiInputTokenLimit?: number;
    aiOutputTokenLimit?: number;
    aiConsentAcceptedAt?: string | null;
};

export type AdminUserCreate = {
    email: string;
    firstName?: string | null;
    lastName?: string | null;
    language: 'en' | 'ru';
    roles: string[];
    temporaryPassword?: string | null;
    generatePassword: boolean;
    isEmailConfirmed: boolean;
    sendCredentialsEmail: boolean;
    requirePasswordChange: boolean;
};

export type AdminUserCreation = {
    user: AdminUser;
    temporaryPassword: string;
    credentialsEmailQueued: boolean;
};

export type AdminUserStatusFilter = 'active' | 'inactive' | 'deleted';

export type AdminUserUpdate = {
    isActive?: boolean | null;
    isEmailConfirmed?: boolean | null;
    roles: string[];
    language?: string | null;
};

export type AdminUserSetPassword = {
    newPassword: string;
};

export type AdminImpersonationStart = {
    code: string;
    targetUserId: AdminId<'user'>;
    targetEmail: string | null;
    actorUserId: AdminId<'user'>;
    reason: string;
};

export type AdminImpersonationSession = {
    id: AdminId<'impersonation-session'>;
    actorUserId: AdminId<'user'>;
    actorEmail: string | null;
    targetUserId: AdminId<'user'>;
    targetEmail: string | null;
    reason: string;
    actorIpAddress?: string | null;
    actorUserAgent?: string | null;
    startedAtUtc: AdminUtcInstant;
};

export type AdminUserLoginEvent = {
    id: AdminId<'login-event'>;
    userId: AdminId<'user'>;
    userEmail: string | null;
    authProvider: string;
    maskedIpAddress?: string | null;
    userAgent?: string | null;
    browserName?: string | null;
    browserVersion?: string | null;
    operatingSystem?: string | null;
    deviceType?: string | null;
    loggedInAtUtc: AdminUtcInstant;
};

export type AdminUserRoleAuditEvent = {
    id: AdminId<'role-audit-event'>;
    userId: AdminId<'user'>;
    roleName: string;
    action: string;
    actorUserId?: AdminId<'user'> | null;
    actorEmail?: string | null;
    source: string;
    occurredAtUtc: AdminUtcInstant;
};

export type AdminUserLoginDeviceSummary = {
    key: string;
    count: number;
    lastSeenAtUtc: AdminUtcInstant;
};

export type PagedResponse<T> = {
    items: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};
