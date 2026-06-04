import type { NutritionFormModel, NutritionMacroState } from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import { CHART_COLORS } from '../../../../../constants/chart-colors';
import { PERCENT_MULTIPLIER } from '../../../../../shared/lib/nutrition.constants';
import { calculateMacroBarState } from '../../../../../shared/lib/nutrition-form.utils';
import type { Product } from '../../../models/product.data';

const MACRO_SUMMARY_LIMIT = 3;
const MIN_MACRO_BAR_PERCENT = 4;
const MIN_MACRO_REFERENCE_VALUE = 1;

export type ProductDetailMacroBlock = {
    labelKey: string;
    value: number;
    unitKey: string;
    color: string;
    percent: number;
};

export type ProductDetailNutritionViewModel = {
    nutritionModel: NutritionFormModel;
    macroBarState: NutritionMacroState;
    macroBlocks: ProductDetailMacroBlock[];
    macroSummaryBlocks: ProductDetailMacroBlock[];
};

export function buildProductDetailNutritionViewModel(product: Product): ProductDetailNutritionViewModel {
    const macroReferenceValues = [product.proteinsPerBase, product.fatsPerBase, product.carbsPerBase];
    const macroBlocks: ProductDetailMacroBlock[] = [
        buildMacroBlock('GENERAL.NUTRIENTS.PROTEIN', product.proteinsPerBase, CHART_COLORS.proteins, macroReferenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.FAT', product.fatsPerBase, CHART_COLORS.fats, macroReferenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.CARB', product.carbsPerBase, CHART_COLORS.carbs, macroReferenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.FIBER', product.fiberPerBase, CHART_COLORS.fiber, macroReferenceValues),
        buildMacroBlock('GENERAL.NUTRIENTS.ALCOHOL', product.alcoholPerBase, CHART_COLORS.alcohol, macroReferenceValues),
    ];

    return {
        nutritionModel: buildNutritionModel(product),
        macroBarState: calculateMacroBarState(product.proteinsPerBase, product.fatsPerBase, product.carbsPerBase),
        macroBlocks,
        macroSummaryBlocks: macroBlocks.slice(0, MACRO_SUMMARY_LIMIT),
    };
}

function buildNutritionModel(product: Product): NutritionFormModel {
    return {
        calories: product.caloriesPerBase,
        proteins: product.proteinsPerBase,
        fats: product.fatsPerBase,
        carbs: product.carbsPerBase,
        fiber: product.fiberPerBase,
        alcohol: product.alcoholPerBase,
    };
}

function buildMacroBlock(labelKey: string, value: number, color: string, referenceValues: number[]): ProductDetailMacroBlock {
    return {
        labelKey,
        value,
        unitKey: 'GENERAL.UNITS.G',
        color,
        percent: resolveMacroPercent(value, referenceValues),
    };
}

function resolveMacroPercent(value: number, values: number[]): number {
    const max = Math.max(...values, value, MIN_MACRO_REFERENCE_VALUE);
    return Math.max(MIN_MACRO_BAR_PERCENT, Math.round((value / max) * PERCENT_MULTIPLIER));
}
