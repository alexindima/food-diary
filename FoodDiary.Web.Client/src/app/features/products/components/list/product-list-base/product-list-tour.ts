import type { LocalizedTourConfig } from '../../../../../shared/tours/localized-tour-definition.service';

export const PRODUCT_LIST_TOUR: LocalizedTourConfig = {
    id: 'product-list',
    translationRoot: 'PRODUCT_LIST.TOUR',
    steps: [
        {
            id: 'actions',
            target: 'product-list-actions',
            titleKey: 'ACTIONS_TITLE',
            descriptionKey: 'ACTIONS_TEXT',
            placement: 'bottom',
        },
        {
            id: 'search',
            target: 'product-list-search',
            titleKey: 'SEARCH_TITLE',
            descriptionKey: 'SEARCH_TEXT',
            placement: 'bottom',
        },
        {
            id: 'favorites',
            target: 'product-list-favorites',
            titleKey: 'FAVORITES_TITLE',
            descriptionKey: 'FAVORITES_TEXT',
            placement: 'top',
        },
        {
            id: 'results',
            target: 'product-list-results',
            titleKey: 'RESULTS_TITLE',
            descriptionKey: 'RESULTS_TEXT',
            placement: 'top',
        },
        {
            id: 'external',
            target: 'product-list-external',
            titleKey: 'EXTERNAL_TITLE',
            descriptionKey: 'EXTERNAL_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'product-list-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
