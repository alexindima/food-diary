import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const FASTING_TOUR: LocalizedTourConfig = {
    id: 'fasting-page',
    translationRoot: 'FASTING.TOUR',
    steps: [
        {
            id: 'timer',
            target: 'fasting-timer',
            titleKey: 'TIMER_TITLE',
            descriptionKey: 'TIMER_TEXT',
            placement: 'right',
        },
        {
            id: 'stats',
            target: 'fasting-stats',
            titleKey: 'STATS_TITLE',
            descriptionKey: 'STATS_TEXT',
            placement: 'left',
        },
        {
            id: 'check-in',
            target: 'fasting-check-in',
            titleKey: 'CHECK_IN_TITLE',
            descriptionKey: 'CHECK_IN_TEXT',
            placement: 'top',
        },
        {
            id: 'insights',
            target: 'fasting-insights',
            titleKey: 'INSIGHTS_TITLE',
            descriptionKey: 'INSIGHTS_TEXT',
            placement: 'top',
        },
        {
            id: 'history',
            target: 'fasting-history',
            titleKey: 'HISTORY_TITLE',
            descriptionKey: 'HISTORY_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'fasting-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
