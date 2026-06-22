import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const WAIST_HISTORY_TOUR: LocalizedTourConfig = {
    id: 'waist-history',
    translationRoot: 'WAIST_HISTORY.TOUR',
    steps: [
        {
            id: 'period',
            target: 'waist-history-period',
            titleKey: 'PERIOD_TITLE',
            descriptionKey: 'PERIOD_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'chart',
            target: 'waist-history-chart',
            titleKey: 'CHART_TITLE',
            descriptionKey: 'CHART_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'entry-form',
            target: 'waist-history-entry-form',
            titleKey: 'ENTRY_FORM_TITLE',
            descriptionKey: 'ENTRY_FORM_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'goal',
            target: 'waist-history-goal',
            titleKey: 'GOAL_TITLE',
            descriptionKey: 'GOAL_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'entries',
            target: 'waist-history-entries',
            titleKey: 'ENTRIES_TITLE',
            descriptionKey: 'ENTRIES_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'waist-history-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
