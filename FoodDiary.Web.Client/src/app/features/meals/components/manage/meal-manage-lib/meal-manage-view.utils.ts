import type { AbstractControl } from '@angular/forms';
import type { TranslateService } from '@ngx-translate/core';

import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';
import type { ConsumptionAiItemManageDto, ConsumptionAiSessionManageDto } from '../../../models/meal.data';
import type { NutritionTotals } from './meal-manage.types';

const FRACTION_EPSILON = 0.01;

const AI_UNIT_TRANSLATION_KEYS: Record<string, string> = {
    g: 'GENERAL.UNITS.G',
    gram: 'GENERAL.UNITS.G',
    grams: 'GENERAL.UNITS.G',
    gr: 'GENERAL.UNITS.G',
    ml: 'GENERAL.UNITS.ML',
    l: 'GENERAL.UNITS.ML',
    pcs: 'GENERAL.UNITS.PCS',
    pc: 'GENERAL.UNITS.PCS',
    piece: 'GENERAL.UNITS.PCS',
    pieces: 'GENERAL.UNITS.PCS',
    kcal: 'GENERAL.UNITS.KCAL',
};

export function formatMealManageAmount(amount: number, unitLabel: string | null, translateService: TranslateService): string {
    const formattedAmount = formatMealManageNumber(amount, translateService);
    return unitLabel !== null ? `${formattedAmount} ${unitLabel}`.trim() : formattedAmount;
}

export function formatMealManageMacro(value: number, unitKey: string, translateService: TranslateService): string {
    const unitLabel = translateService.instant(unitKey);
    return `${formatMealManageNumber(value, translateService)} ${unitLabel}`.trim();
}

export function formatMealManageNumber(value: number, translateService: TranslateService): string {
    const hasFraction = Math.abs(value % 1) > FRACTION_EPSILON;
    return new Intl.NumberFormat(getMealManageLanguage(translateService), {
        maximumFractionDigits: hasFraction ? 1 : 0,
        minimumFractionDigits: hasFraction ? 1 : 0,
    }).format(value);
}

export function formatMealAiAmount(amount: number, unit: string, translateService: TranslateService): string {
    const normalized = unit.trim().toLowerCase();
    const unitKey = normalized.length > 0 ? AI_UNIT_TRANSLATION_KEYS[normalized] : undefined;
    const unitLabel = unitKey !== undefined ? translateService.instant(unitKey) : unit;
    return unitLabel.length > 0 ? `${amount} ${unitLabel}`.trim() : `${amount}`;
}

export function formatMealAiName(name?: string | null): string {
    const trimmed = name?.trim() ?? '';
    if (trimmed.length === 0) {
        return '';
    }

    const [first, ...rest] = trimmed;
    return `${first.toLocaleUpperCase()}${rest.join('')}`;
}

export function getMealManageLanguage(translateService: TranslateService): string {
    const currentLang = translateService.getCurrentLang();
    if (currentLang.length > 0) {
        return currentLang;
    }

    const fallbackLang = translateService.getFallbackLang() ?? '';
    return fallbackLang.length > 0 ? fallbackLang : 'en';
}

export function getEmptyNutritionTotals(): NutritionTotals {
    return { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 };
}

export function getAiSessionTotals(session: ConsumptionAiSessionManageDto): NutritionTotals {
    return session.items.reduce((totals, item) => addAiItemTotals(totals, item), getEmptyNutritionTotals());
}

export function resolveMealManageControlError(
    control: AbstractControl | null,
    translateService: TranslateService,
    minFallback = 0,
): string | null {
    if (control === null || control.invalid === false || control.touched === false) {
        return null;
    }

    if (control.errors?.['required'] === true) {
        return translateService.instant('FORM_ERRORS.REQUIRED');
    }

    const minError: unknown = control.getError('min');
    if (minError !== null) {
        const min = getNumberProperty(minError, 'min') ?? minFallback;
        return translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
    }

    return translateService.instant('FORM_ERRORS.UNKNOWN');
}

function addAiItemTotals(totals: NutritionTotals, item: ConsumptionAiItemManageDto): NutritionTotals {
    return {
        calories: totals.calories + item.calories,
        proteins: totals.proteins + item.proteins,
        fats: totals.fats + item.fats,
        carbs: totals.carbs + item.carbs,
        fiber: totals.fiber + item.fiber,
        alcohol: totals.alcohol + item.alcohol,
    };
}
