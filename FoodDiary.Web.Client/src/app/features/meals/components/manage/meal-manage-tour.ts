import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const MEAL_MANAGE_TOUR: LocalizedTourConfig = {
    id: 'meal-manage',
    translationRoot: 'CONSUMPTION_MANAGE.TOUR',
    steps: [
        {
            id: 'general',
            target: 'meal-manage-general',
            titleKey: 'GENERAL_TITLE',
            descriptionKey: 'GENERAL_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'items',
            target: 'meal-manage-items',
            titleKey: 'ITEMS_TITLE',
            descriptionKey: 'ITEMS_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'satiety',
            target: 'meal-manage-satiety',
            titleKey: 'SATIETY_TITLE',
            descriptionKey: 'SATIETY_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'nutrition',
            target: 'meal-manage-nutrition',
            titleKey: 'NUTRITION_TITLE',
            descriptionKey: 'NUTRITION_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'help',
            target: 'meal-manage-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
