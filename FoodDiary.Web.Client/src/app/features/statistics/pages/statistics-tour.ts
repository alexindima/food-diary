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
            id: 'help',
            target: 'statistics-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
