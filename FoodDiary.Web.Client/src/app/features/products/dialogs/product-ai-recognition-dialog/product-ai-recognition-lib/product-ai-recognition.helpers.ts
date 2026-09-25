import { HttpStatusCode } from '@angular/common/http';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';
import type { FoodNutritionResponse, FoodVisionItem, ProductLabel } from '../../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../../models/product.data';
import type { ProductAiRecognitionFormModel, ProductAiRecognitionResult } from '../product-ai-recognition-dialog.types';

const MILLILITERS_PER_LITER = 1000;

export function isProductAiRecognitionModelValid(model: ProductAiRecognitionFormModel): boolean {
    const nutrients = [model.proteinsPerBase, model.fatsPerBase, model.carbsPerBase, model.fiberPerBase, model.alcoholPerBase];
    return (
        model.name.trim().length > 0 &&
        model.portionAmount !== null &&
        Number.isFinite(model.portionAmount) &&
        model.portionAmount > 0 &&
        model.baseUnit !== null &&
        Object.values(MeasurementUnit).includes(model.baseUnit) &&
        model.caloriesPerBase !== null &&
        Number.isFinite(model.caloriesPerBase) &&
        model.caloriesPerBase >= 0 &&
        nutrients.every(value => value === null || (Number.isFinite(value) && value >= 0))
    );
}

export function createProductAiRecognitionFormModel(): ProductAiRecognitionFormModel {
    return {
        name: '',
        brand: '',
        portionAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        baseUnit: MeasurementUnit.G,
        caloriesPerBase: 0,
        proteinsPerBase: 0,
        fatsPerBase: 0,
        carbsPerBase: 0,
        fiberPerBase: 0,
        alcoholPerBase: 0,
    };
}

export type ProductAiRecognitionResultBuildParams = {
    model: ProductAiRecognitionFormModel;
    selection: ImageSelection | null;
    itemNames: readonly string[];
    results: readonly FoodVisionItem[];
    description: string | null;
};

export function buildProductAiRecognitionResult(params: ProductAiRecognitionResultBuildParams): ProductAiRecognitionResult {
    const { model, selection, itemNames, results, description } = params;
    const name = model.name.trim();
    const baseUnit = model.baseUnit ?? MeasurementUnit.G;
    const requestedBaseAmount = getNumericValue(model.portionAmount ?? 0);
    const baseAmount = requestedBaseAmount > 0 ? requestedBaseAmount : getRecognizedAmount(results, baseUnit);

    return {
        name: name.length > 0 ? name : (itemNames[0] ?? ''),
        description,
        ...(model.brand.trim().length > 0 ? { brand: model.brand.trim() } : {}),
        image: selection !== null ? { ...selection } : null,
        baseAmount,
        baseUnit,
        caloriesPerBase: getNumericValue(model.caloriesPerBase ?? 0),
        proteinsPerBase: getOptionalNumericValue(model.proteinsPerBase),
        fatsPerBase: getOptionalNumericValue(model.fatsPerBase),
        carbsPerBase: getOptionalNumericValue(model.carbsPerBase),
        fiberPerBase: getOptionalNumericValue(model.fiberPerBase),
        alcoholPerBase: getOptionalNumericValue(model.alcoholPerBase),
    };
}

export function buildProductAiRecognitionModelFromNutrition(
    items: readonly FoodVisionItem[],
    nutrition: FoodNutritionResponse,
): ProductAiRecognitionFormModel {
    const primary = items.length > 0 ? items[0] : null;
    const name = primary === null ? '' : capitalizeName(primary.nameLocal?.trim() ?? primary.nameEn.trim());
    const baseUnit = resolveAiMeasurementUnit(primary?.unit);

    return {
        name,
        brand: '',
        portionAmount: getRecognizedAmount(items, baseUnit),
        baseUnit,
        caloriesPerBase: nutrition.calories,
        proteinsPerBase: nutrition.protein,
        fatsPerBase: nutrition.fat,
        carbsPerBase: nutrition.carbs,
        fiberPerBase: nutrition.fiber,
        alcoholPerBase: nutrition.alcohol,
    };
}

export function normalizeItemsForNutrition(items: readonly FoodVisionItem[]): FoodVisionItem[] {
    return items.map(item => {
        const baseUnit = resolveAiMeasurementUnit(item.unit);
        const normalizedUnit = baseUnit === MeasurementUnit.PCS ? 'pcs' : baseUnit.toLowerCase();
        const amount = getNumericValue(item.amount) * getUnitScale(item.unit);
        const normalizedAmount = amount > 0 ? amount : getDefaultBaseAmount(baseUnit);

        return {
            ...item,
            amount: normalizedAmount,
            unit: normalizedUnit,
        };
    });
}

export function resolveAiMeasurementUnit(unit?: string | null): MeasurementUnit {
    if (unit === null || unit === undefined || unit.length === 0) {
        return MeasurementUnit.G;
    }

    const normalized = unit.trim().toLowerCase();
    if (['g', 'gram', 'grams', 'gr'].includes(normalized)) {
        return MeasurementUnit.G;
    }
    if (['ml', 'l', 'liter', 'liters'].includes(normalized)) {
        return MeasurementUnit.ML;
    }
    if (['pcs', 'pc', 'piece', 'pieces'].includes(normalized)) {
        return MeasurementUnit.PCS;
    }
    return MeasurementUnit.G;
}

export function getRecognizedAmount(items: readonly FoodVisionItem[], unit: MeasurementUnit): number {
    const compatibleAmounts = items
        .filter(item => resolveAiMeasurementUnit(item.unit) === unit)
        .map(item => getNumericValue(item.amount) * getUnitScale(item.unit))
        .filter(amount => amount > 0);

    if (compatibleAmounts.length > 0) {
        return compatibleAmounts.reduce((total, amount) => total + amount, 0);
    }

    return getDefaultBaseAmount(unit);
}

function getUnitScale(unit: string): number {
    return ['l', 'liter', 'liters'].includes(unit.trim().toLowerCase()) ? MILLILITERS_PER_LITER : 1;
}

export function mapAiRecognitionErrorKey(error: unknown): string {
    const status = getNumberProperty(error, 'status');
    if (status === HttpStatusCode.Forbidden) {
        return 'PRODUCT_AI_DIALOG.ERROR_PREMIUM';
    }
    if (status === HttpStatusCode.TooManyRequests) {
        return 'PRODUCT_AI_DIALOG.ERROR_QUOTA';
    }
    return 'PRODUCT_AI_DIALOG.ERROR_GENERIC';
}

export function mapAiNutritionErrorKey(error: unknown): string {
    const status = getNumberProperty(error, 'status');
    return status === HttpStatusCode.TooManyRequests ? 'PRODUCT_AI_DIALOG.ERROR_QUOTA' : 'PRODUCT_AI_DIALOG.NUTRITION_ERROR';
}

export function capitalizeName(value: string): string {
    if (value.length === 0) {
        return '';
    }
    return value.charAt(0).toUpperCase() + value.slice(1);
}

function getDefaultBaseAmount(unit: MeasurementUnit): number {
    return unit === MeasurementUnit.PCS ? 1 : DEFAULT_NUTRITION_BASE_AMOUNT;
}

function getNumericValue(value: number | string): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
}

export function buildProductLabelFormModel(label: ProductLabel): ProductAiRecognitionFormModel {
    return {
        name: label.name ?? '',
        brand: label.brand ?? '',
        portionAmount: getOptionalNumericValue(label.baseAmount),
        baseUnit: (label.baseUnit === null) ? null : resolveAiMeasurementUnit(label.baseUnit),
        caloriesPerBase: getOptionalNumericValue(label.calories),
        proteinsPerBase: getOptionalNumericValue(label.protein),
        fatsPerBase: getOptionalNumericValue(label.fat),
        carbsPerBase: getOptionalNumericValue(label.carbs),
        fiberPerBase: getOptionalNumericValue(label.fiber),
        alcoholPerBase: getOptionalNumericValue(label.alcohol),
    };
}

function getOptionalNumericValue(value: number | null): number | null {
    return value === null ? null : getNumericValue(value);
}
