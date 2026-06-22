import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const SHOPPING_LIST_TOUR: LocalizedTourConfig = {
    id: 'shopping-list',
    translationRoot: 'SHOPPING_LIST.TOUR',
    steps: [
        {
            id: 'summary',
            target: 'shopping-list-summary',
            titleKey: 'SUMMARY_TITLE',
            descriptionKey: 'SUMMARY_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'items',
            target: 'shopping-list-items',
            titleKey: 'ITEMS_TITLE',
            descriptionKey: 'ITEMS_DESCRIPTION',
            placement: 'right',
        },
        {
            id: 'manage',
            target: 'shopping-list-manage',
            titleKey: 'MANAGE_TITLE',
            descriptionKey: 'MANAGE_DESCRIPTION',
            placement: 'left',
        },
        {
            id: 'help',
            target: 'shopping-list-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
