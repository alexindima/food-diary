import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const WEIGHT_HISTORY_TOUR: LocalizedTourConfig = {
    id: 'weight-history',
    translationRoot: 'WEIGHT_HISTORY.TOUR',
    steps: [
        {
            id: 'period',
            target: 'weight-history-period',
            titleKey: 'PERIOD_TITLE',
            descriptionKey: 'PERIOD_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'chart',
            target: 'weight-history-chart',
            titleKey: 'CHART_TITLE',
            descriptionKey: 'CHART_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'entry-form',
            target: 'weight-history-entry-form',
            titleKey: 'ENTRY_FORM_TITLE',
            descriptionKey: 'ENTRY_FORM_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'goal',
            target: 'weight-history-goal',
            titleKey: 'GOAL_TITLE',
            descriptionKey: 'GOAL_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'entries',
            target: 'weight-history-entries',
            titleKey: 'ENTRIES_TITLE',
            descriptionKey: 'ENTRIES_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'weight-history-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
