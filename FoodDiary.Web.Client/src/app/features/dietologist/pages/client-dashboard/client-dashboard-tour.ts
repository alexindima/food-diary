import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const CLIENT_DASHBOARD_TOUR: LocalizedTourConfig = {
    id: 'dietologist-client-dashboard',
    translationRoot: 'DIETOLOGIST.CLIENT_DASHBOARD.TOUR',
    steps: [
        {
            id: 'profile',
            target: 'client-dashboard-profile',
            titleKey: 'PROFILE_TITLE',
            descriptionKey: 'PROFILE_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'period',
            target: 'client-dashboard-period',
            titleKey: 'PERIOD_TITLE',
            descriptionKey: 'PERIOD_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'metrics',
            target: 'client-dashboard-metrics',
            titleKey: 'METRICS_TITLE',
            descriptionKey: 'METRICS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'recommendations',
            target: 'client-dashboard-recommendations',
            titleKey: 'RECOMMENDATIONS_TITLE',
            descriptionKey: 'RECOMMENDATIONS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'client-dashboard-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
