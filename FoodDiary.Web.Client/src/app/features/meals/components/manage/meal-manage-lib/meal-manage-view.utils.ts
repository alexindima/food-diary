import type { TranslateService } from '@ngx-translate/core';

import { resolveTranslateLanguage } from '../../../../../shared/i18n/translate-language.utils';
import type { MealAiItemManageDto, MealAiSessionManageDto } from '../../../../../shared/models/meal.data';
import type { NutritionTotals } from './meal-manage.types';

const FRACTION_EPSILON = 0.01;

const AI_UNIT_TRANSLATION_KEYS = new Map([
    ['g', 'GENERAL.UNITS.G'],
    ['gram', 'GENERAL.UNITS.G'],
    ['grams', 'GENERAL.UNITS.G'],
    ['gr', 'GENERAL.UNITS.G'],
    ['ml', 'GENERAL.UNITS.ML'],
    ['pcs', 'GENERAL.UNITS.PCS'],
    ['pc', 'GENERAL.UNITS.PCS'],
    ['piece', 'GENERAL.UNITS.PCS'],
    ['pieces', 'GENERAL.UNITS.PCS'],
    ['kcal', 'GENERAL.UNITS.KCAL'],
]);

export function formatMealManageAmount(amount: number, unitLabel: string, translateService: TranslateService): string {
    return `${formatMealManageNumber(amount, translateService)} ${unitLabel}`.trim();
}

export function formatMealManageMacro(value: number, unitKey: string, translateService: TranslateService): string {
    return formatMealManageAmount(value, translateService.instant(unitKey), translateService);
}

export function formatMealManageNumber(value: number, translateService: TranslateService): string {
    const hasFraction = Math.abs(value % 1) > FRACTION_EPSILON;
    return new Intl.NumberFormat(getMealManageLanguage(translateService), {
        maximumFractionDigits: hasFraction ? 1 : 0,
        minimumFractionDigits: hasFraction ? 1 : 0,
    }).format(value);
}

export function formatMealAiAmount(amount: number, unit: string, translateService: TranslateService): string {
    const unitKey = AI_UNIT_TRANSLATION_KEYS.get(unit.trim().toLowerCase());
    const unitLabel = unitKey !== undefined ? translateService.instant(unitKey) : unit;
    const formattedAmount = new Intl.NumberFormat(getMealManageLanguage(translateService), { maximumFractionDigits: 20 }).format(amount);
    return `${formattedAmount} ${unitLabel}`.trim();
}

export function formatMealAiName(name: string): string {
    const trimmed = name.trim();
    if (trimmed.length === 0) {
        return '';
    }

    const [first, ...rest] = trimmed;
    return `${first.toLocaleUpperCase()}${rest.join('')}`;
}

export function getMealManageLanguage(translateService: TranslateService): string {
    return resolveTranslateLanguage(translateService);
}

export function getEmptyNutritionTotals(): NutritionTotals {
    return { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 };
}

export function getAiSessionTotals(session: MealAiSessionManageDto): NutritionTotals {
    return session.items
        .filter(item => item.resolution !== 'Rejected')
        .reduce((totals, item) => addAiItemTotals(totals, item), getEmptyNutritionTotals());
}

function addAiItemTotals(totals: NutritionTotals, item: MealAiItemManageDto): NutritionTotals {
    return {
        calories: totals.calories + item.calories,
        proteins: totals.proteins + item.proteins,
        fats: totals.fats + item.fats,
        carbs: totals.carbs + item.carbs,
        fiber: totals.fiber + item.fiber,
        alcohol: totals.alcohol + item.alcohol,
    };
}
