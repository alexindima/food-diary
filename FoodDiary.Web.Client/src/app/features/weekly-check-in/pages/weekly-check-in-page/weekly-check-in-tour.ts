import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const WEEKLY_CHECK_IN_TOUR: LocalizedTourConfig = {
    id: 'weekly-check-in',
    translationRoot: 'WEEKLY_CHECK_IN.TOUR',
    steps: [
        {
            id: 'trends',
            target: 'weekly-check-in-trends',
            titleKey: 'TRENDS_TITLE',
            descriptionKey: 'TRENDS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'stats',
            target: 'weekly-check-in-stats',
            titleKey: 'STATS_TITLE',
            descriptionKey: 'STATS_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'suggestions',
            target: 'weekly-check-in-suggestions',
            titleKey: 'SUGGESTIONS_TITLE',
            descriptionKey: 'SUGGESTIONS_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'help',
            target: 'weekly-check-in-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
