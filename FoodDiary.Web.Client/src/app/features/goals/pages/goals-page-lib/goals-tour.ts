import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const GOALS_TOUR: LocalizedTourConfig = {
    id: 'goals-page',
    translationRoot: 'GOALS_PAGE.TOUR',
    steps: [
        {
            id: 'calories',
            target: 'goals-calories',
            titleKey: 'CALORIES_TITLE',
            descriptionKey: 'CALORIES_TEXT',
            placement: 'right',
        },
        {
            id: 'cycling',
            target: 'goals-cycling',
            titleKey: 'CYCLING_TITLE',
            descriptionKey: 'CYCLING_TEXT',
            placement: 'right',
        },
        {
            id: 'water',
            target: 'goals-water',
            titleKey: 'WATER_TITLE',
            descriptionKey: 'WATER_TEXT',
            placement: 'right',
        },
        {
            id: 'macros',
            target: 'goals-macros',
            titleKey: 'MACROS_TITLE',
            descriptionKey: 'MACROS_TEXT',
            placement: 'left',
        },
        {
            id: 'body',
            target: 'goals-body',
            titleKey: 'BODY_TITLE',
            descriptionKey: 'BODY_TEXT',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'goals-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
