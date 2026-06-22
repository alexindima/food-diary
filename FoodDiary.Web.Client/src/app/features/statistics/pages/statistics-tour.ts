import type { LocalizedTourConfig } from '../../../shared/tours/localized-tour-definition.service';

export const STATISTICS_TOUR: LocalizedTourConfig = {
    id: 'statistics-page',
    translationRoot: 'STATISTICS.TOUR',
    steps: [
        {
            id: 'period',
            target: 'statistics-period',
            titleKey: 'PERIOD_TITLE',
            descriptionKey: 'PERIOD_TEXT',
            placement: 'bottom',
        },
        {
            id: 'summary',
            target: 'statistics-summary',
            titleKey: 'SUMMARY_TITLE',
            descriptionKey: 'SUMMARY_TEXT',
            placement: 'top',
        },
        {
            id: 'nutrition',
            target: 'statistics-nutrition',
            titleKey: 'NUTRITION_TITLE',
            descriptionKey: 'NUTRITION_TEXT',
            placement: 'top',
        },
        {
            id: 'body',
            target: 'statistics-body',
            titleKey: 'BODY_TITLE',
            descriptionKey: 'BODY_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'statistics-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
