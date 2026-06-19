import { MeasurementUnit } from '../models/product.data';

export const PRODUCT_NAME_SEARCH_MIN_LENGTH = 3;
export const PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT = 5;
export const PRODUCT_MIN_AMOUNT = 0.001;
export const PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT = 10000;
export const PRODUCT_MAX_PIECE_AMOUNT = 1000;
export const PRODUCT_WEIGHT_OR_VOLUME_BASE_AMOUNT = 100;
export const PRODUCT_NAME_MAX_LENGTH = 256;
export const PRODUCT_BARCODE_MAX_LENGTH = 128;
export const PRODUCT_BRAND_MAX_LENGTH = 128;
export const PRODUCT_DESCRIPTION_MAX_LENGTH = 2048;
export const PRODUCT_COMMENT_MAX_LENGTH = 2048;
export const PRODUCT_MAX_WEIGHT_OR_VOLUME_CALORIES_PER_BASE = 1000;
export const PRODUCT_MAX_WEIGHT_OR_VOLUME_NUTRIENT_PER_BASE = 100;
export const PRODUCT_MAX_PIECE_CALORIES_PER_BASE = 5000;
export const PRODUCT_MAX_PIECE_NUTRIENT_PER_BASE = 1000;
export const PRODUCT_NUTRIENT_ROUNDING_FACTOR = 10;

export function getProductMaxAmountForUnit(unit: MeasurementUnit | null | undefined): number {
    return unit === MeasurementUnit.PCS ? PRODUCT_MAX_PIECE_AMOUNT : PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT;
}

export function getProductMaxCaloriesPerBaseForUnit(unit: MeasurementUnit | null | undefined): number {
    return unit === MeasurementUnit.PCS ? PRODUCT_MAX_PIECE_CALORIES_PER_BASE : PRODUCT_MAX_WEIGHT_OR_VOLUME_CALORIES_PER_BASE;
}

export function getProductMaxNutrientPerBaseForUnit(unit: MeasurementUnit | null | undefined): number {
    return unit === MeasurementUnit.PCS ? PRODUCT_MAX_PIECE_NUTRIENT_PER_BASE : PRODUCT_MAX_WEIGHT_OR_VOLUME_NUTRIENT_PER_BASE;
}

export function getProductMaxNutritionDisplayForUnit(maxPerBase: number, unit: MeasurementUnit | null | undefined): number {
    const baseAmount = unit === MeasurementUnit.PCS ? 1 : PRODUCT_WEIGHT_OR_VOLUME_BASE_AMOUNT;
    return maxPerBase * (getProductMaxAmountForUnit(unit) / baseAmount);
}
