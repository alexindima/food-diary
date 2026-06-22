import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const EXPLORE_TOUR: LocalizedTourConfig = {
    id: 'explore',
    translationRoot: 'EXPLORE.TOUR',
    steps: [
        {
            id: 'search',
            target: 'explore-search',
            titleKey: 'SEARCH_TITLE',
            descriptionKey: 'SEARCH_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'sort',
            target: 'explore-sort',
            titleKey: 'SORT_TITLE',
            descriptionKey: 'SORT_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'recipes',
            target: 'explore-recipes',
            titleKey: 'RECIPES_TITLE',
            descriptionKey: 'RECIPES_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'explore-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
