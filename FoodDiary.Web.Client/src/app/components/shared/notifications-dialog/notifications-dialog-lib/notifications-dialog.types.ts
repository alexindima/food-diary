import type { NotificationItem } from '../../../../services/notification.service';

export type NotificationViewModel = {
    notification: NotificationItem;
    isPasswordSetupSuggestion: boolean;
    isDietologistInvitation: boolean;
    hasAccentIcon: boolean;
    icon: string;
    badgeKey: string | null;
    actionKey: string | null;
    ariaLabel: string;
    dateLabel: string;
};
