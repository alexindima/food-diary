export type NotificationItem = {
    id: string;
    type: string;
    title: string;
    body: string | null;
    targetUrl: string | null;
    referenceId: string | null;
    isRead: boolean;
    createdAtUtc: string;
};

export type ScheduleTestNotificationRequest = {
    delaySeconds: number;
    type: string;
};

export type ScheduledNotificationResponse = {
    type: string;
    delaySeconds: number;
    scheduledAtUtc: string;
};

export type WebPushConfiguration = {
    enabled: boolean;
    publicKey: string | null;
};

export type WebPushSubscriptionRequest = {
    endpoint: string;
    expirationTime: string | null;
    keys: {
        p256dh: string;
        auth: string;
    };
    locale: string | null;
    userAgent: string | null;
};

export type NotificationPreferences = {
    pushNotificationsEnabled: boolean;
    fastingPushNotificationsEnabled: boolean;
    socialPushNotificationsEnabled: boolean;
    fastingCheckInReminderHours: number;
    fastingCheckInFollowUpReminderHours: number;
};

export type WebPushSubscriptionItem = {
    endpoint: string;
    endpointHost: string;
    expirationTimeUtc: string | null;
    locale: string | null;
    userAgent: string | null;
    createdAtUtc: string;
    updatedAtUtc: string | null;
};

export type UpdateNotificationPreferencesRequest = {
    pushNotificationsEnabled?: boolean;
    fastingPushNotificationsEnabled?: boolean;
    socialPushNotificationsEnabled?: boolean;
    fastingCheckInReminderHours?: number;
    fastingCheckInFollowUpReminderHours?: number;
};
