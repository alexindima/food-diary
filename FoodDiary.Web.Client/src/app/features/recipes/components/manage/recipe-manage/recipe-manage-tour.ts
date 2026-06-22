import type { LocalizedTourConfig } from '../../../../../shared/tours/localized-tour-definition.service';

export const RECIPE_MANAGE_TOUR: LocalizedTourConfig = {
    id: 'recipe-manage',
    translationRoot: 'RECIPE_MANAGE.TOUR',
    steps: [
        {
            id: 'basic-info',
            target: 'recipe-manage-basic-info',
            titleKey: 'BASIC_INFO_TITLE',
            descriptionKey: 'BASIC_INFO_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'steps',
            target: 'recipe-manage-steps',
            titleKey: 'STEPS_TITLE',
            descriptionKey: 'STEPS_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'nutrition',
            target: 'recipe-manage-nutrition',
            titleKey: 'NUTRITION_TITLE',
            descriptionKey: 'NUTRITION_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'actions',
            target: 'recipe-manage-actions',
            titleKey: 'ACTIONS_TITLE',
            descriptionKey: 'ACTIONS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'recipe-manage-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
