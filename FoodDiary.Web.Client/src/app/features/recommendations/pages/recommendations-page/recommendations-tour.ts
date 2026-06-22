import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const RECOMMENDATIONS_TOUR: LocalizedTourConfig = {
    id: 'recommendations',
    translationRoot: 'RECOMMENDATIONS.TOUR',
    steps: [
        {
            id: 'list',
            target: 'recommendations-list',
            titleKey: 'LIST_TITLE',
            descriptionKey: 'LIST_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'help',
            target: 'recommendations-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
