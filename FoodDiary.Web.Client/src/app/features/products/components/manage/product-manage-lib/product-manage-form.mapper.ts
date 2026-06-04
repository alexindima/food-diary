import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { getControlNumericValue } from '../../../../../shared/lib/nutrition-form.utils';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { ProductAiRecognitionResult } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types';
import { PRODUCT_NUTRIENT_ROUNDING_FACTOR } from '../../../lib/product-manage.constants';
import { normalizeProductType as normalizeProductTypeValue } from '../../../lib/product-type.utils';
import { type CreateProductRequest, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import type { NutritionMode, ProductFormValues } from './product-manage-form.types';

export type NutritionValues = {
    caloriesPerBase: number | null;
    proteinsPerBase: number | null;
    fatsPerBase: number | null;
    carbsPerBase: number | null;
    fiberPerBase: number | null;
    alcoholPerBase: number | null;
};

export type ProductNutritionValues = {
    caloriesPerBase: number;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
};

export type NutritionField = keyof NutritionValues;

export const PRODUCT_NUTRITION_FIELDS: readonly NutritionField[] = [
    'caloriesPerBase',
    'proteinsPerBase',
    'fatsPerBase',
    'carbsPerBase',
    'fiberPerBase',
    'alcoholPerBase',
];

export function createProductForm(): ProductFormValues {
    return {
        name: '',
        barcode: null,
        brand: null,
        productType: ProductType.Unknown,
        description: null,
        comment: null,
        imageUrl: null,
        baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        defaultPortionAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        baseUnit: MeasurementUnit.G,
        caloriesPerBase: null,
        proteinsPerBase: null,
        fatsPerBase: null,
        carbsPerBase: null,
        fiberPerBase: null,
        alcoholPerBase: null,
        visibility: ProductVisibility.Private,
        usdaFdcId: null,
    };
}

export function getDefaultProductBaseAmount(unit: MeasurementUnit): number {
    return unit === MeasurementUnit.PCS ? 1 : DEFAULT_NUTRITION_BASE_AMOUNT;
}

export function getProductControlNumberValue(value: number | string | null): number {
    return getControlNumericValue({ value });
}

export function buildProductData(values: ProductFormValues, nutritionMode: NutritionMode): CreateProductRequest {
    const baseAmount = getDefaultProductBaseAmount(values.baseUnit);
    const defaultPortionAmount = getProductControlNumberValue(values.defaultPortionAmount);
    const normalizeFactor = nutritionMode === 'portion' && defaultPortionAmount > 0 ? baseAmount / defaultPortionAmount : 1;
    const nutritionValues = getNormalizedNutritionValues(values, normalizeFactor);
    const imageSelection = values.imageUrl;
    const productType = values.productType;

    return {
        name: values.name,
        barcode: values.barcode,
        brand: values.brand,
        productType,
        category: productType,
        description: values.description,
        comment: values.comment,
        imageUrl: imageSelection?.url ?? null,
        imageAssetId: imageSelection?.assetId ?? null,
        baseAmount,
        defaultPortionAmount,
        baseUnit: values.baseUnit,
        ...nutritionValues,
        visibility: values.visibility,
    };
}

export function buildConvertedNutritionPatch(values: ProductFormValues, factor: number): Partial<ProductFormValues> {
    const patch: Partial<ProductFormValues> = {};
    PRODUCT_NUTRITION_FIELDS.forEach(field => {
        const rawValue = values[field];
        if (rawValue === null) {
            return;
        }

        patch[field] = roundProductNutrientValue(getProductControlNumberValue(rawValue) * factor);
    });

    return patch;
}

export function buildProductFormPatch(product: Product): Partial<ProductFormValues> {
    const normalizedVisibility = normalizeProductVisibility(product.visibility);
    const normalizedProductType = normalizeProductTypeValue(product.productType ?? product.category ?? null) ?? ProductType.Unknown;
    const targetBaseAmount = getDefaultProductBaseAmount(product.baseUnit);
    const normalizedNutrition = normalizeProductNutritionValues(
        {
            caloriesPerBase: product.caloriesPerBase,
            proteinsPerBase: product.proteinsPerBase,
            fatsPerBase: product.fatsPerBase,
            carbsPerBase: product.carbsPerBase,
            fiberPerBase: product.fiberPerBase,
            alcoholPerBase: product.alcoholPerBase,
        },
        product.baseAmount,
        targetBaseAmount,
    );

    return {
        name: product.name,
        barcode: product.barcode ?? null,
        brand: product.brand ?? null,
        productType: normalizedProductType,
        description: product.description ?? null,
        comment: product.comment ?? null,
        imageUrl: getProductImageSelection(product),
        baseAmount: targetBaseAmount,
        defaultPortionAmount: product.defaultPortionAmount,
        baseUnit: product.baseUnit,
        caloriesPerBase: normalizedNutrition.caloriesPerBase,
        proteinsPerBase: normalizedNutrition.proteinsPerBase,
        fatsPerBase: normalizedNutrition.fatsPerBase,
        carbsPerBase: normalizedNutrition.carbsPerBase,
        fiberPerBase: normalizedNutrition.fiberPerBase,
        alcoholPerBase: normalizedNutrition.alcoholPerBase,
        visibility: normalizedVisibility,
        usdaFdcId: product.usdaFdcId ?? null,
    };
}

export function buildAiResultPatch(values: ProductFormValues, result: ProductAiRecognitionResult): Partial<ProductFormValues> {
    const targetBaseAmount = getDefaultProductBaseAmount(result.baseUnit);
    const portionAmount = result.baseAmount > 0 ? result.baseAmount : targetBaseAmount;

    return {
        name: result.name.length > 0 ? result.name : values.name,
        description: result.description ?? values.description,
        imageUrl: result.image ?? values.imageUrl,
        baseAmount: targetBaseAmount,
        baseUnit: result.baseUnit,
        caloriesPerBase: roundNullableProductNutrientValue(result.caloriesPerBase),
        proteinsPerBase: roundNullableProductNutrientValue(result.proteinsPerBase),
        fatsPerBase: roundNullableProductNutrientValue(result.fatsPerBase),
        carbsPerBase: roundNullableProductNutrientValue(result.carbsPerBase),
        fiberPerBase: roundNullableProductNutrientValue(result.fiberPerBase),
        alcoholPerBase: roundNullableProductNutrientValue(result.alcoholPerBase),
        defaultPortionAmount: portionAmount,
    };
}

export function buildResetNutritionPatch(): Partial<ProductFormValues> {
    return {
        caloriesPerBase: null,
        proteinsPerBase: null,
        fatsPerBase: null,
        carbsPerBase: null,
        fiberPerBase: null,
        alcoholPerBase: null,
    };
}

export function normalizeProductNutritionValues(
    values: NutritionValues,
    sourceAmount: number | null,
    targetAmount: number,
): NutritionValues {
    if (sourceAmount === null || sourceAmount <= 0 || sourceAmount === targetAmount) {
        return roundProductNutritionValues(values, 1);
    }

    return roundProductNutritionValues(values, targetAmount / sourceAmount);
}

export function roundNullableProductNutrientValue(value: number | null): number | null {
    return value === null ? null : roundProductNutrientValue(value);
}

export function roundProductNutrientValue(value: number): number {
    return Math.round(value * PRODUCT_NUTRIENT_ROUNDING_FACTOR) / PRODUCT_NUTRIENT_ROUNDING_FACTOR;
}

function getNormalizedNutritionValues(values: ProductFormValues, normalizeFactor: number): ProductNutritionValues {
    return {
        caloriesPerBase: roundProductNutrientValue(getProductControlNumberValue(values.caloriesPerBase) * normalizeFactor),
        proteinsPerBase: roundProductNutrientValue(getProductControlNumberValue(values.proteinsPerBase) * normalizeFactor),
        fatsPerBase: roundProductNutrientValue(getProductControlNumberValue(values.fatsPerBase) * normalizeFactor),
        carbsPerBase: roundProductNutrientValue(getProductControlNumberValue(values.carbsPerBase) * normalizeFactor),
        fiberPerBase: roundProductNutrientValue(getProductControlNumberValue(values.fiberPerBase) * normalizeFactor),
        alcoholPerBase: roundProductNutrientValue(getProductControlNumberValue(values.alcoholPerBase) * normalizeFactor),
    };
}

function roundProductNutritionValues(values: NutritionValues, factor: number): NutritionValues {
    return {
        caloriesPerBase: roundOptionalProductNutrientValue(values.caloriesPerBase, factor),
        proteinsPerBase: roundOptionalProductNutrientValue(values.proteinsPerBase, factor),
        fatsPerBase: roundOptionalProductNutrientValue(values.fatsPerBase, factor),
        carbsPerBase: roundOptionalProductNutrientValue(values.carbsPerBase, factor),
        fiberPerBase: roundOptionalProductNutrientValue(values.fiberPerBase, factor),
        alcoholPerBase: roundOptionalProductNutrientValue(values.alcoholPerBase, factor),
    };
}

function roundOptionalProductNutrientValue(value: number | null, factor: number): number | null {
    return value === null ? null : roundProductNutrientValue(value * factor);
}

function getProductImageSelection(product: Product): ImageSelection {
    return {
        url: product.imageUrl ?? null,
        assetId: product.imageAssetId ?? null,
    };
}

function normalizeProductVisibility(value: ProductVisibility | null | string | undefined): ProductVisibility {
    if (typeof value !== 'string') {
        return ProductVisibility.Private;
    }

    return value.toUpperCase() === ProductVisibility.Public.toUpperCase() ? ProductVisibility.Public : ProductVisibility.Private;
}
