import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const CYCLE_TRACKING_TOUR: LocalizedTourConfig = {
    id: 'cycle-tracking',
    translationRoot: 'CYCLE_TRACKING.TOUR',
    steps: [
        {
            id: 'setup',
            target: 'cycle-tracking-setup',
            titleKey: 'SETUP_TITLE',
            descriptionKey: 'SETUP_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'current',
            target: 'cycle-tracking-current',
            titleKey: 'CURRENT_TITLE',
            descriptionKey: 'CURRENT_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'day-log',
            target: 'cycle-tracking-day-log',
            titleKey: 'DAY_LOG_TITLE',
            descriptionKey: 'DAY_LOG_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'factors',
            target: 'cycle-tracking-factors',
            titleKey: 'FACTORS_TITLE',
            descriptionKey: 'FACTORS_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'history',
            target: 'cycle-tracking-history',
            titleKey: 'HISTORY_TITLE',
            descriptionKey: 'HISTORY_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'cycle-tracking-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
