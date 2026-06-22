import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const MEAL_LIST_TOUR: LocalizedTourConfig = {
    id: 'meal-list',
    translationRoot: 'CONSUMPTION_LIST.TOUR',
    steps: [
        {
            id: 'filters',
            target: 'meal-list-filters',
            titleKey: 'FILTERS_TITLE',
            descriptionKey: 'FILTERS_TEXT',
            placement: 'bottom',
        },
        {
            id: 'create',
            target: 'meal-list-create',
            titleKey: 'CREATE_TITLE',
            descriptionKey: 'CREATE_TEXT',
            placement: 'bottom',
        },
        {
            id: 'favorites',
            target: 'meal-list-favorites',
            titleKey: 'FAVORITES_TITLE',
            descriptionKey: 'FAVORITES_TEXT',
            placement: 'top',
        },
        {
            id: 'results',
            target: 'meal-list-results',
            titleKey: 'RESULTS_TITLE',
            descriptionKey: 'RESULTS_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'meal-list-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
