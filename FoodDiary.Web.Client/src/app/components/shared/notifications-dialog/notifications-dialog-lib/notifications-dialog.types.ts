import type { NotificationItem } from '../../../../shared/notifications/notification.service';

export type NotificationViewModel = {
    notification: NotificationItem;
    isPasswordSetupSuggestion: boolean;
    isDietologistInvitation: boolean;
    isDietologistRecommendation: boolean;
    hasAccentIcon: boolean;
    icon: string;
    badgeKey: string | null;
    actionKey: string | null;
    ariaLabel: string;
    dateLabel: string;
};
