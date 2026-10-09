import type { KnownNotificationCode, NotificationKind } from '../../../../shared/models/notification-kind';

export type NotificationPresentation = {
    readonly isPasswordSetupSuggestion: boolean;
    readonly isDietologistInvitation: boolean;
    readonly isDietologistRecommendation: boolean;
    readonly hasAccentIcon: boolean;
    readonly icon: string;
    readonly badgeKey: string | null;
    readonly actionKey: string | null;
};

const GENERIC_PRESENTATION: NotificationPresentation = {
    isPasswordSetupSuggestion: false,
    isDietologistInvitation: false,
    isDietologistRecommendation: false,
    hasAccentIcon: false,
    icon: 'notifications',
    badgeKey: null,
    actionKey: null,
};
const INVITATION_PRESENTATION: NotificationPresentation = {
    ...GENERIC_PRESENTATION,
    isDietologistInvitation: true,
    hasAccentIcon: true,
    icon: 'medical_information',
    badgeKey: 'NOTIFICATIONS.DIETOLOGIST_INVITATION_BADGE',
    actionKey: 'NOTIFICATIONS.DIETOLOGIST_INVITATION_ACTION',
};
const RECOMMENDATION_PRESENTATION: NotificationPresentation = {
    ...GENERIC_PRESENTATION,
    isDietologistRecommendation: true,
    hasAccentIcon: true,
    icon: 'medical_information',
    badgeKey: 'NOTIFICATIONS.RECOMMENDATION_BADGE',
    actionKey: 'NOTIFICATIONS.RECOMMENDATION_ACTION',
};
const TASK_PRESENTATION: NotificationPresentation = {
    ...GENERIC_PRESENTATION,
    hasAccentIcon: true,
    icon: 'task_alt',
    badgeKey: 'NOTIFICATIONS.TASK_BADGE',
    actionKey: 'NOTIFICATIONS.TASK_ACTION',
};
const PASSWORD_PRESENTATION: NotificationPresentation = {
    ...GENERIC_PRESENTATION,
    isPasswordSetupSuggestion: true,
    hasAccentIcon: true,
    icon: 'password',
    badgeKey: 'NOTIFICATIONS.PASSWORD_SETUP_BADGE',
    actionKey: 'NOTIFICATIONS.PASSWORD_SETUP_ACTION',
};

const PRESENTATIONS: Readonly<Record<KnownNotificationCode, NotificationPresentation>> = {
    NewRecommendation: RECOMMENDATION_PRESENTATION,
    NewRecommendationComment: RECOMMENDATION_PRESENTATION,
    NewRecommendationCommentForDietologist: RECOMMENDATION_PRESENTATION,
    NewClientTask: TASK_PRESENTATION,
    ClientTaskChangedForDietologist: TASK_PRESENTATION,
    ClientTaskCancelled: TASK_PRESENTATION,
    ClientTaskDueSoon: TASK_PRESENTATION,
    PasswordSetupSuggested: PASSWORD_PRESENTATION,
    DietologistInvitationReceived: INVITATION_PRESENTATION,
    DietologistInvitationAccepted: GENERIC_PRESENTATION,
    DietologistInvitationDeclined: GENERIC_PRESENTATION,
    NewComment: GENERIC_PRESENTATION,
    FastingCompleted: GENERIC_PRESENTATION,
    EatingWindowStarted: GENERIC_PRESENTATION,
    FastingWindowStarted: GENERIC_PRESENTATION,
    FastingCheckInReminder: GENERIC_PRESENTATION,
    WeeklyCheckIn: GENERIC_PRESENTATION,
    WeeklyGoalReminder: GENERIC_PRESENTATION,
    Hydration: GENERIC_PRESENTATION,
    GoalReached: GENERIC_PRESENTATION,
    Lesson: GENERIC_PRESENTATION,
    MealPlan: GENERIC_PRESENTATION,
    Achievement: GENERIC_PRESENTATION,
};

export function notificationPresentation(type: NotificationKind): NotificationPresentation {
    return type.kind === 'known' ? PRESENTATIONS[type.code] : GENERIC_PRESENTATION;
}
