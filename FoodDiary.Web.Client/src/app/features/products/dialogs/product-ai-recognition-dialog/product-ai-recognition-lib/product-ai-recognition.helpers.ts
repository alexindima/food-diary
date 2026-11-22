import { HttpStatusCode } from '@angular/common/http';

import { AI_CONSENT_ERROR_KEY, isAiConsentRequiredError } from '../../../../../shared/lib/ai-consent-error.utils';
import { getAiTrialQuotaErrorKey } from '../../../../../shared/lib/ai-quota-error.utils';
import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';
import type { FoodNutritionResponse, FoodVisionItem, ProductLabel } from '../../../../../shared/models/ai.data';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { MeasurementUnit } from '../../../../../shared/models/product.data';
import type { ProductAiRecognitionFormModel, ProductAiRecognitionResult } from '../product-ai-recognition-dialog.types';

const MILLILITERS_PER_LITER = 1000;

type ValidProductAiRecognitionModel = ProductAiRecognitionFormModel & {
    portionAmount: number;
    baseUnit: MeasurementUnit;
    caloriesPerBase: number;
};

export function isProductAiRecognitionModelValid(model: ProductAiRecognitionFormModel): model is ValidProductAiRecognitionModel {
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
    model: ValidProductAiRecognitionModel;
    selection: ImageSelection | null;
    description: string | null;
};

export function buildProductAiRecognitionResult(params: ProductAiRecognitionResultBuildParams): ProductAiRecognitionResult {
    const { model, selection, description } = params;
    const brand = model.brand.trim();

    return {
        name: model.name.trim(),
        description,
        ...(brand.length > 0 ? { brand } : {}),
        image: selection !== null ? { ...selection } : null,
        baseAmount: model.portionAmount,
        baseUnit: model.baseUnit,
        caloriesPerBase: model.caloriesPerBase,
        proteinsPerBase: model.proteinsPerBase,
        fatsPerBase: model.fatsPerBase,
        carbsPerBase: model.carbsPerBase,
        fiberPerBase: model.fiberPerBase,
        alcoholPerBase: model.alcoholPerBase,
    };
}

export function buildProductAiRecognitionModelFromNutrition(
    items: readonly FoodVisionItem[],
    nutrition: FoodNutritionResponse,
): ProductAiRecognitionFormModel {
    const primary = items.length > 0 ? items[0] : null;
    const name = primary === null ? '' : getProductRecognitionItemName(primary);
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
        const amount = item.amount * getUnitScale(item.unit);
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
        .map(item => item.amount * getUnitScale(item.unit))
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
    if (isAiConsentRequiredError(error)) {
        return AI_CONSENT_ERROR_KEY;
    }
    const status = getNumberProperty(error, 'status');
    if (status === HttpStatusCode.Forbidden) {
        return 'PRODUCT_AI_DIALOG.ERROR_PREMIUM';
    }
    if (status === HttpStatusCode.TooManyRequests) {
        return getAiTrialQuotaErrorKey(error) ?? 'PRODUCT_AI_DIALOG.ERROR_QUOTA';
    }
    return 'PRODUCT_AI_DIALOG.ERROR_GENERIC';
}

export function mapAiNutritionErrorKey(error: unknown): string {
    const status = getNumberProperty(error, 'status');
    return (
        getAiTrialQuotaErrorKey(error) ??
        (status === HttpStatusCode.TooManyRequests ? 'PRODUCT_AI_DIALOG.ERROR_QUOTA' : 'PRODUCT_AI_DIALOG.NUTRITION_ERROR')
    );
}

export function getProductRecognitionItemName(item: FoodVisionItem): string {
    const localizedName = item.nameLocal?.trim() ?? '';
    const value = localizedName.length > 0 ? localizedName : item.nameEn.trim();
    if (value.length === 0) {
        return '';
    }
    return value.charAt(0).toUpperCase() + value.slice(1);
}

function getDefaultBaseAmount(unit: MeasurementUnit): number {
    return unit === MeasurementUnit.PCS ? 1 : DEFAULT_NUTRITION_BASE_AMOUNT;
}

export function buildProductLabelFormModel(label: ProductLabel): ProductAiRecognitionFormModel {
    return {
        name: label.name ?? '',
        brand: label.brand ?? '',
        portionAmount: label.baseAmount,
        baseUnit: label.baseUnit === null ? null : resolveAiMeasurementUnit(label.baseUnit),
        caloriesPerBase: label.calories,
        proteinsPerBase: label.protein,
        fatsPerBase: label.fat,
        carbsPerBase: label.carbs,
        fiberPerBase: label.fiber,
        alcoholPerBase: label.alcohol,
    };
}
