import type { TranslateService } from '@ngx-translate/core';
import type { FdTourDefinition } from 'fd-tour';

const DASHBOARD_WELCOME_TOUR_VERSION = 2;

export function buildDashboardWelcomeTour(translateService: TranslateService): FdTourDefinition {
    return {
        id: 'dashboard-welcome',
        version: DASHBOARD_WELCOME_TOUR_VERSION,
        labels: {
            previous: translateTourText(translateService, 'LABELS.PREVIOUS'),
            next: translateTourText(translateService, 'LABELS.NEXT'),
            finish: translateTourText(translateService, 'LABELS.FINISH'),
            skip: translateTourText(translateService, 'LABELS.SKIP'),
            close: translateTourText(translateService, 'LABELS.CLOSE'),
        },
        steps: [
            {
                id: 'quick-add',
                target: '[data-tour-id="dashboard-quick-add"]',
                title: translateTourText(translateService, 'QUICK_ADD_TITLE'),
                description: translateTourText(translateService, 'QUICK_ADD_TEXT'),
                placement: 'bottom',
            },
            {
                id: 'summary',
                target: '[data-tour-id="dashboard-summary"]',
                title: translateTourText(translateService, 'SUMMARY_TITLE'),
                description: translateTourText(translateService, 'SUMMARY_TEXT'),
                placement: 'right',
            },
            {
                id: 'meals',
                target: '[data-tour-id="dashboard-meals"]',
                title: translateTourText(translateService, 'MEALS_TITLE'),
                description: translateTourText(translateService, 'MEALS_TEXT'),
                placement: 'top',
            },
            {
                id: 'hydration',
                target: '[data-tour-id="dashboard-hydration"]',
                title: translateTourText(translateService, 'HYDRATION_TITLE'),
                description: translateTourText(translateService, 'HYDRATION_TEXT'),
                placement: 'left',
            },
            {
                id: 'appearance',
                target: '[data-tour-id="dashboard-appearance"]',
                title: translateTourText(translateService, 'APPEARANCE_TITLE'),
                description: translateTourText(translateService, 'APPEARANCE_TEXT'),
                placement: 'bottom',
            },
            {
                id: 'header-actions',
                target: '[data-tour-id="dashboard-layout-settings"]',
                title: translateTourText(translateService, 'SETTINGS_TITLE'),
                description: translateTourText(translateService, 'SETTINGS_TEXT'),
                placement: 'bottom',
            },
            {
                id: 'help',
                target: '[data-tour-id="dashboard-tour-help"]',
                title: translateTourText(translateService, 'HELP_TITLE'),
                description: translateTourText(translateService, 'HELP_TEXT'),
                placement: 'bottom',
            },
        ],
    };
}

function translateTourText(translateService: TranslateService, key: string): string {
    const translation = translateService.instant(`DASHBOARD.TOUR.${key}`);
    return typeof translation === 'string' ? translation : key;
}
