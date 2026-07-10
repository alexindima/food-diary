export type AdminUser = {
    id: string;
    email: string;
    hasPassword?: boolean;
    username?: string | null;
    firstName?: string | null;
    lastName?: string | null;
    birthDate?: string | null;
    gender?: string | null;
    weight?: number | null;
    desiredWeight?: number | null;
    desiredWaist?: number | null;
    height?: number | null;
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
    profileImageAssetId?: string | null;
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
    createdOnUtc: string;
    deletedAt?: string | null;
    lastLoginAtUtc?: string | null;
    roles: string[];
    aiInputTokenLimit?: number;
    aiOutputTokenLimit?: number;
    aiConsentAcceptedAt?: string | null;
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
    accessToken: string;
    targetUserId: string;
    targetEmail: string;
    actorUserId: string;
    reason: string;
};

export type AdminImpersonationSession = {
    id: string;
    actorUserId: string;
    actorEmail: string;
    targetUserId: string;
    targetEmail: string;
    reason: string;
    actorIpAddress?: string | null;
    actorUserAgent?: string | null;
    startedAtUtc: string;
};

export type AdminUserLoginEvent = {
    id: string;
    userId: string;
    userEmail: string;
    authProvider: string;
    maskedIpAddress?: string | null;
    userAgent?: string | null;
    browserName?: string | null;
    browserVersion?: string | null;
    operatingSystem?: string | null;
    deviceType?: string | null;
    loggedInAtUtc: string;
};

export type AdminUserRoleAuditEvent = {
    id: string;
    userId: string;
    roleName: string;
    action: string;
    actorUserId?: string | null;
    actorEmail?: string | null;
    source: string;
    occurredAtUtc: string;
};

export type AdminUserLoginDeviceSummary = {
    key: string;
    count: number;
    lastSeenAtUtc: string;
};

export type PagedResponse<T> = {
    items: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};
