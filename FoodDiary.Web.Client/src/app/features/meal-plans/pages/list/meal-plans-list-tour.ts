import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const MEAL_PLANS_LIST_TOUR: LocalizedTourConfig = {
    id: 'meal-plans-list',
    translationRoot: 'MEAL_PLANS.TOUR',
    steps: [
        {
            id: 'filters',
            target: 'meal-plans-filters',
            titleKey: 'FILTERS_TITLE',
            descriptionKey: 'FILTERS_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'plans',
            target: 'meal-plans-list',
            titleKey: 'PLANS_TITLE',
            descriptionKey: 'PLANS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'meal-plans-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
