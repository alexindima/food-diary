import type { TranslateService } from '@ngx-translate/core';
import type { FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { MEAL_TYPE_OPTIONS } from '../../../../shared/lib/meal-type.util';

export type MealSatietyControlName = 'preMealSatietyLevel' | 'postMealSatietyLevel';

export function buildMealTypeSelectOptions(translateService: TranslateService): Array<FdUiSelectOption<string>> {
    return MEAL_TYPE_OPTIONS.map(option => ({
        value: option,
        label: translateService.instant(`MEAL_TYPES.${option}`),
    }));
}

export function buildMealNutritionModeOptions(translateService: TranslateService): FdUiSegmentedToggleOption[] {
    return [
        {
            value: 'auto',
            label: translateService.instant('CONSUMPTION_MANAGE.NUTRITION_MODE.AUTO'),
        },
        {
            value: 'manual',
            label: translateService.instant('CONSUMPTION_MANAGE.NUTRITION_MODE.MANUAL'),
        },
    ];
}
