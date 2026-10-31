export type AdminTestNotificationType = 'FastingCompleted' | 'FastingCheckInReminder' | 'EatingWindowStarted' | 'FastingWindowStarted';

export type AdminTestNotification = {
    type: AdminTestNotificationType;
    delaySeconds: number;
};
