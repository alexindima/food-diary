import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const USER_MANAGE_TOUR: LocalizedTourConfig = {
    id: 'user-manage',
    translationRoot: 'USER_MANAGE.TOUR',
    steps: [
        {
            id: 'account',
            target: 'user-manage-account',
            titleKey: 'ACCOUNT_TITLE',
            descriptionKey: 'ACCOUNT_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'billing',
            target: 'user-manage-billing',
            titleKey: 'BILLING_TITLE',
            descriptionKey: 'BILLING_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'body',
            target: 'user-manage-body',
            titleKey: 'BODY_TITLE',
            descriptionKey: 'BODY_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'notifications',
            target: 'user-manage-notifications',
            titleKey: 'NOTIFICATIONS_TITLE',
            descriptionKey: 'NOTIFICATIONS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'dietologist',
            target: 'user-manage-dietologist',
            titleKey: 'DIETOLOGIST_TITLE',
            descriptionKey: 'DIETOLOGIST_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'privacy',
            target: 'user-manage-privacy',
            titleKey: 'PRIVACY_TITLE',
            descriptionKey: 'PRIVACY_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'user-manage-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
