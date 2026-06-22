import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const MEAL_PLAN_DETAIL_TOUR: LocalizedTourConfig = {
    id: 'meal-plan-detail',
    translationRoot: 'MEAL_PLANS.DETAIL_TOUR',
    steps: [
        {
            id: 'header',
            target: 'meal-plan-detail-header',
            titleKey: 'HEADER_TITLE',
            descriptionKey: 'HEADER_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'days',
            target: 'meal-plan-detail-days',
            titleKey: 'DAYS_TITLE',
            descriptionKey: 'DAYS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'meal-plan-detail-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
