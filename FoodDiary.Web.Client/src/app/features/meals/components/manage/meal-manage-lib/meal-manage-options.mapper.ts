import type { TranslateService } from '@ngx-translate/core';
import type { FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { MEAL_TYPE_OPTIONS, type MealTypeOption } from '../../../../../shared/lib/meal-type.util';
import type { NutritionMode } from './meal-manage.types';

export type MealSatietyControlName = 'preMealSatietyLevel' | 'postMealSatietyLevel';

export function buildMealTypeSelectOptions(translateService: TranslateService): Array<FdUiSelectOption<MealTypeOption>> {
    return MEAL_TYPE_OPTIONS.map(option => ({
        value: option,
        label: translateService.instant(`MEAL_TYPES.${option}`),
    }));
}

export function buildMealNutritionModeOptions(translateService: TranslateService): Array<FdUiSegmentedToggleOption<NutritionMode>> {
    return [
        {
            value: 'auto',
            label: translateService.instant('MEAL_MANAGE.NUTRITION_MODE.AUTO'),
        },
        {
            value: 'manual',
            label: translateService.instant('MEAL_MANAGE.NUTRITION_MODE.MANUAL'),
        },
    ];
}
