import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const PREMIUM_ACCESS_TOUR: LocalizedTourConfig = {
    id: 'premium-access',
    translationRoot: 'PREMIUM_PAGE.TOUR',
    steps: [
        {
            id: 'overview',
            target: 'premium-overview',
            titleKey: 'OVERVIEW_TITLE',
            descriptionKey: 'OVERVIEW_DESCRIPTION',
            placement: 'bottom',
        },
        {
            id: 'plans',
            target: 'premium-plans',
            titleKey: 'PLANS_TITLE',
            descriptionKey: 'PLANS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'benefits',
            target: 'premium-benefits',
            titleKey: 'BENEFITS_TITLE',
            descriptionKey: 'BENEFITS_DESCRIPTION',
            placement: 'top',
        },
        {
            id: 'help',
            target: 'premium-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_DESCRIPTION',
            placement: 'left',
        },
    ],
};
