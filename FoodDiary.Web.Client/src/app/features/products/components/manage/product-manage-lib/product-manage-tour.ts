import type { TranslateService } from '@ngx-translate/core';
import type { FdTourDefinition } from 'fd-tour';

const PRODUCT_MANAGE_TOUR_VERSION = 1;

export function buildProductManageTour(translateService: TranslateService): FdTourDefinition {
    return {
        id: 'product-manage',
        version: PRODUCT_MANAGE_TOUR_VERSION,
        labels: {
            previous: translateTourText(translateService, 'LABELS.PREVIOUS'),
            next: translateTourText(translateService, 'LABELS.NEXT'),
            finish: translateTourText(translateService, 'LABELS.FINISH'),
            skip: translateTourText(translateService, 'LABELS.SKIP'),
            close: translateTourText(translateService, 'LABELS.CLOSE'),
        },
        steps: [
            {
                id: 'basic',
                target: '[data-tour-id="product-manage-basic"]',
                title: translateTourText(translateService, 'BASIC_TITLE'),
                description: translateTourText(translateService, 'BASIC_TEXT'),
                placement: 'right',
            },
            {
                id: 'nutrition',
                target: '[data-tour-id="product-manage-nutrition"]',
                title: translateTourText(translateService, 'NUTRITION_TITLE'),
                description: translateTourText(translateService, 'NUTRITION_TEXT'),
                placement: 'left',
            },
            {
                id: 'tracking',
                target: '[data-tour-id="product-manage-tracking"]',
                title: translateTourText(translateService, 'TRACKING_TITLE'),
                description: translateTourText(translateService, 'TRACKING_TEXT'),
                placement: 'top',
            },
            {
                id: 'actions',
                target: '[data-tour-id="product-manage-actions"]',
                title: translateTourText(translateService, 'ACTIONS_TITLE'),
                description: translateTourText(translateService, 'ACTIONS_TEXT'),
                placement: 'top',
            },
            {
                id: 'help',
                target: '[data-tour-id="product-manage-tour-help"]',
                title: translateTourText(translateService, 'HELP_TITLE'),
                description: translateTourText(translateService, 'HELP_TEXT'),
                placement: 'bottom',
            },
        ],
    };
}

function translateTourText(translateService: TranslateService, key: string): string {
    const translation = translateService.instant(`PRODUCT_MANAGE.TOUR.${key}`);
    return typeof translation === 'string' ? translation : key;
}
