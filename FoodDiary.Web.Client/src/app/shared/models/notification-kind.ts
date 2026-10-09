const KNOWN_NOTIFICATION_CODES = [
    'NewRecommendation',
    'NewRecommendationComment',
    'NewRecommendationCommentForDietologist',
    'NewClientTask',
    'ClientTaskChangedForDietologist',
    'ClientTaskCancelled',
    'ClientTaskDueSoon',
    'PasswordSetupSuggested',
    'DietologistInvitationReceived',
    'DietologistInvitationAccepted',
    'DietologistInvitationDeclined',
    'NewComment',
    'FastingCompleted',
    'EatingWindowStarted',
    'FastingWindowStarted',
    'FastingCheckInReminder',
    'WeeklyCheckIn',
    'WeeklyGoalReminder',
    'Hydration',
    'GoalReached',
    'Lesson',
    'MealPlan',
    'Achievement',
] as const;

export type KnownNotificationCode = (typeof KNOWN_NOTIFICATION_CODES)[number];
export type NotificationKind =
    { readonly kind: 'known'; readonly code: KnownNotificationCode } | { readonly kind: 'unknown'; readonly code: string };

function isKnownNotificationCode(code: string): code is KnownNotificationCode {
    const codes: readonly string[] = KNOWN_NOTIFICATION_CODES;
    return codes.includes(code);
}

/** Preserve unknown server codes so newer notifications retain the generic presentation. */
export function decodeNotificationKind(code: string): NotificationKind {
    return isKnownNotificationCode(code) ? { kind: 'known', code } : { kind: 'unknown', code };
}
