import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const RECIPE_LIST_TOUR: LocalizedTourConfig = {
    id: 'recipe-list',
    translationRoot: 'RECIPE_LIST.TOUR',
    steps: [
        {
            id: 'actions',
            target: 'recipe-list-actions',
            titleKey: 'ACTIONS_TITLE',
            descriptionKey: 'ACTIONS_TEXT',
            placement: 'bottom',
        },
        {
            id: 'search',
            target: 'recipe-list-search',
            titleKey: 'SEARCH_TITLE',
            descriptionKey: 'SEARCH_TEXT',
            placement: 'bottom',
        },
        {
            id: 'favorites',
            target: 'recipe-list-favorites',
            titleKey: 'FAVORITES_TITLE',
            descriptionKey: 'FAVORITES_TEXT',
            placement: 'top',
        },
        {
            id: 'results',
            target: 'recipe-list-results',
            titleKey: 'RESULTS_TITLE',
            descriptionKey: 'RESULTS_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'recipe-list-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
