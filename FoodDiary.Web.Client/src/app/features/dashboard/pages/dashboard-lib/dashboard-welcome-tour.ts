import type { LocalizedTourConfig } from '../../../../shared/tours/localized-tour-definition.service';

export const DASHBOARD_WELCOME_TOUR: LocalizedTourConfig = {
    id: 'dashboard-welcome',
    translationRoot: 'DASHBOARD.TOUR',
    steps: [
        {
            id: 'quick-add',
            target: 'dashboard-quick-add',
            titleKey: 'QUICK_ADD_TITLE',
            descriptionKey: 'QUICK_ADD_TEXT',
            placement: 'bottom',
        },
        {
            id: 'summary',
            target: 'dashboard-summary',
            titleKey: 'SUMMARY_TITLE',
            descriptionKey: 'SUMMARY_TEXT',
            placement: 'right',
        },
        {
            id: 'meals',
            target: 'dashboard-meals',
            titleKey: 'MEALS_TITLE',
            descriptionKey: 'MEALS_TEXT',
            placement: 'top',
        },
        {
            id: 'hydration',
            target: 'dashboard-hydration',
            titleKey: 'HYDRATION_TITLE',
            descriptionKey: 'HYDRATION_TEXT',
            placement: 'left',
        },
        {
            id: 'appearance',
            target: 'dashboard-appearance',
            titleKey: 'APPEARANCE_TITLE',
            descriptionKey: 'APPEARANCE_TEXT',
            placement: 'bottom',
        },
        {
            id: 'header-actions',
            target: 'dashboard-layout-settings',
            titleKey: 'SETTINGS_TITLE',
            descriptionKey: 'SETTINGS_TEXT',
            placement: 'bottom',
        },
        {
            id: 'help',
            target: 'dashboard-tour-help',
            titleKey: 'HELP_TITLE',
            descriptionKey: 'HELP_TEXT',
            placement: 'bottom',
        },
    ],
};
