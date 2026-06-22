import type { LocalizedTourConfig } from '../../../../../shared/tours/localized-tour-definition.service';

export const PRODUCT_MANAGE_TOUR: LocalizedTourConfig = {
    id: 'product-manage',
    translationRoot: 'PRODUCT_MANAGE.TOUR',
    steps: [
        {
            id: 'basic',
            target: 'product-manage-basic',
            titleKey: 'BASIC_TITLE',
            descriptionKey: 'BASIC_TEXT',
            placement: 'right',
        },
        {
            id: 'nutrition',
            target: 'product-manage-nutrition',
            titleKey: 'NUTRITION_TITLE',
            descriptionKey: 'NUTRITION_TEXT',
            placement: 'left',
        },
        {
            id: 'tracking',
            target: 'product-manage-tracking',
            titleKey: 'TRACKING_TITLE',
            descriptionKey: 'TRACKING_TEXT',
            placement: 'top',
        },
        {
            id: 'actions',
            target: 'product-manage-actions',
            titleKey: 'ACTIONS_TITLE',
            descriptionKey: 'ACTIONS_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'product-manage-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
