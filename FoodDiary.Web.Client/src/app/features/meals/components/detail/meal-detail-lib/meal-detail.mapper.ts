import { FormControl, FormGroup } from '@angular/forms';
import { DEFAULT_HUNGER_LEVELS, DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';

import type {
    NutritionControlNames,
    NutritionMacroState,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { CHART_COLORS } from '../../../../../constants/chart-colors';
import { PERCENT_MULTIPLIER } from '../../../../../shared/lib/nutrition.constants';
import { normalizeSatietyLevel } from '../../../../../shared/lib/satiety-level.utils';
import type { ConsumptionAiItem, Meal } from '../../../models/meal.data';
import { MEAL_DETAIL_DEFAULT_SATIETY_EMOJI, MEAL_DETAIL_MIN_MACRO_BAR_PERCENT } from './meal-detail.config';
import type { MealDetailItemPreview, MealMacroBlock, MealSatietyMeta } from './meal-detail.types';

export type MealDetailNutritionForm = {
    calories: FormControl<number | null>;
    proteins: FormControl<number | null>;
    fats: FormControl<number | null>;
    carbs: FormControl<number | null>;
    fiber: FormControl<number | null>;
    alcohol: FormControl<number | null>;
};

export type MealDetailViewModel = {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
    mealTypeLabel: string | null;
    preMealSatietyMeta: MealSatietyMeta;
    postMealSatietyMeta: MealSatietyMeta;
    itemPreview: MealDetailItemPreview[];
    macroBlocks: MealMacroBlock[];
    nutritionControlNames: NutritionControlNames;
    nutritionForm: FormGroup<MealDetailNutritionForm>;
    macroBarState: NutritionMacroState;
};

export function buildMealDetailViewModel(meal: Meal, translate: (key: string) => string): MealDetailViewModel {
    const calories = meal.totalCalories;
    const proteins = meal.totalProteins;
    const fats = meal.totalFats;
    const carbs = meal.totalCarbs;
    const fiber = meal.totalFiber;
    const alcohol = meal.totalAlcohol;
    const datasetValues = [proteins, fats, carbs];

    return {
        calories,
        proteins,
        fats,
        carbs,
        fiber,
        alcohol,
        mealTypeLabel:
            meal.mealType !== null && meal.mealType !== undefined && meal.mealType.length > 0
                ? translate(`MEAL_TYPES.${meal.mealType}`)
                : null,
        preMealSatietyMeta: buildSatietyMeta('before', meal.preMealSatietyLevel, translate),
        postMealSatietyMeta: buildSatietyMeta('after', meal.postMealSatietyLevel, translate),
        itemPreview: buildItemPreview(meal, translate),
        macroBlocks: buildMacroBlocks({ proteins, fats, carbs, fiber, alcohol }, datasetValues),
        nutritionControlNames: {
            calories: 'calories',
            proteins: 'proteins',
            fats: 'fats',
            carbs: 'carbs',
            fiber: 'fiber',
            alcohol: 'alcohol',
        },
        nutritionForm: buildNutritionForm({ calories, proteins, fats, carbs, fiber, alcohol }),
        macroBarState: buildMacroBarState(datasetValues),
    };
}

function buildNutritionForm(values: {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
}): FormGroup<MealDetailNutritionForm> {
    return new FormGroup<MealDetailNutritionForm>({
        calories: new FormControl(values.calories),
        proteins: new FormControl(values.proteins),
        fats: new FormControl(values.fats),
        carbs: new FormControl(values.carbs),
        fiber: new FormControl(values.fiber),
        alcohol: new FormControl(values.alcohol),
    });
}

function buildMacroBarState(values: number[]): NutritionMacroState {
    const total = values.reduce((sum, value) => sum + value, 0);

    return {
        isEmpty: total <= 0,
        segments: [
            { key: 'proteins', percent: total > 0 ? (values[0] / total) * PERCENT_MULTIPLIER : 0 },
            { key: 'fats', percent: total > 0 ? (values[1] / total) * PERCENT_MULTIPLIER : 0 },
            { key: 'carbs', percent: total > 0 ? (values[2] / total) * PERCENT_MULTIPLIER : 0 },
        ],
    };
}

function buildMacroBlocks(
    values: { proteins: number; fats: number; carbs: number; fiber: number; alcohol: number },
    datasetValues: number[],
): MealMacroBlock[] {
    return [
        {
            labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
            value: values.proteins,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.proteins,
            percent: resolveMacroPercent(values.proteins, datasetValues),
        },
        {
            labelKey: 'GENERAL.NUTRIENTS.FAT',
            value: values.fats,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.fats,
            percent: resolveMacroPercent(values.fats, datasetValues),
        },
        {
            labelKey: 'GENERAL.NUTRIENTS.CARB',
            value: values.carbs,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.carbs,
            percent: resolveMacroPercent(values.carbs, datasetValues),
        },
        {
            labelKey: 'GENERAL.NUTRIENTS.FIBER',
            value: values.fiber,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.fiber,
            percent: resolveMacroPercent(values.fiber, datasetValues),
        },
        {
            labelKey: 'GENERAL.NUTRIENTS.ALCOHOL',
            value: values.alcohol,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.alcohol,
            percent: resolveMacroPercent(values.alcohol, datasetValues),
        },
    ];
}

function resolveMacroPercent(value: number, values: number[]): number {
    const max = Math.max(...values, value, 1);
    return Math.max(MEAL_DETAIL_MIN_MACRO_BAR_PERCENT, Math.round((value / max) * PERCENT_MULTIPLIER));
}

function buildItemPreview(meal: Meal, translate: (key: string) => string): MealDetailItemPreview[] {
    const manualItems = meal.items.map(item => {
        const unit = item.product?.baseUnit;
        return {
            name: item.product?.name ?? item.recipe?.name ?? translate('CONSUMPTION_DETAIL.SUMMARY.UNKNOWN_ITEM'),
            amount: item.amount,
            unitKey: unit !== undefined ? `GENERAL.UNITS.${unit}` : 'CONSUMPTION_DETAIL.SERVINGS',
            unitText: null,
        };
    });
    const aiItems = (meal.aiSessions ?? []).flatMap(session =>
        session.items.map(item => {
            const unitKey = getAiItemUnitKey(item.unit);
            return {
                name: getAiItemName(item, translate),
                amount: item.amount,
                unitKey,
                unitText: unitKey !== null ? null : item.unit,
            };
        }),
    );

    return [...manualItems, ...aiItems];
}

function getAiItemName(item: ConsumptionAiItem, translate: (key: string) => string): string {
    const itemName = item.nameLocal?.trim() ?? item.nameEn.trim();
    return itemName.length > 0 ? itemName : translate('CONSUMPTION_DETAIL.SUMMARY.UNKNOWN_ITEM');
}

function getAiItemUnitKey(unit: string): string | null {
    const normalized = unit.trim().toLowerCase();
    const unitMap: Record<string, string> = {
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
    };

    return unitMap[normalized] ?? null;
}

function buildSatietyMeta(kind: 'before' | 'after', value: number | null | undefined, translate: (key: string) => string): MealSatietyMeta {
    if (typeof value !== 'number') {
        return {
            emoji: MEAL_DETAIL_DEFAULT_SATIETY_EMOJI,
            title: translate('HUNGER_SCALE.LEVEL_0.TITLE'),
            description: translate('HUNGER_SCALE.LEVEL_0.DESCRIPTION'),
        };
    }

    const normalizedValue = normalizeSatietyLevel(Math.round(value));
    const levels = kind === 'before' ? DEFAULT_HUNGER_LEVELS : DEFAULT_SATIETY_LEVELS;
    const level = levels.find(item => item.value === normalizedValue);

    return {
        emoji: level?.emoji ?? MEAL_DETAIL_DEFAULT_SATIETY_EMOJI,
        title: translate(level?.titleKey ?? 'HUNGER_SCALE.LEVEL_0.TITLE'),
        description: translate(level?.descriptionKey ?? 'HUNGER_SCALE.LEVEL_0.DESCRIPTION'),
    };
}
