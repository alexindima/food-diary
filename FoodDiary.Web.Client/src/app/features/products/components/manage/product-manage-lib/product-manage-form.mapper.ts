import { FormControl, FormGroup, Validators } from '@angular/forms';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { getControlNumericValue } from '../../../../../shared/lib/nutrition-form.utils';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { ProductAiRecognitionResult } from '../../../dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types';
import { PRODUCT_MIN_AMOUNT, PRODUCT_NUTRIENT_ROUNDING_FACTOR } from '../../../lib/product-manage.constants';
import { normalizeProductType as normalizeProductTypeValue } from '../../../lib/product-type.utils';
import { type CreateProductRequest, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import type { NutritionMode, ProductFormData, ProductFormValues } from './product-manage-form.types';

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

export function createProductForm(): FormGroup<ProductFormData> {
    return new FormGroup<ProductFormData>({
        name: new FormControl('', { nonNullable: true, validators: Validators.required }),
        barcode: new FormControl(null),
        brand: new FormControl(null),
        productType: new FormControl<ProductType>(ProductType.Unknown, { nonNullable: true }),
        description: new FormControl(null),
        comment: new FormControl(null),
        imageUrl: new FormControl<ImageSelection | null>(null),
        baseAmount: new FormControl(DEFAULT_NUTRITION_BASE_AMOUNT, {
            nonNullable: true,
            validators: [Validators.required, Validators.min(PRODUCT_MIN_AMOUNT)],
        }),
        defaultPortionAmount: new FormControl(DEFAULT_NUTRITION_BASE_AMOUNT, {
            nonNullable: true,
            validators: [Validators.required, Validators.min(PRODUCT_MIN_AMOUNT)],
        }),
        baseUnit: new FormControl(MeasurementUnit.G, { nonNullable: true, validators: Validators.required }),
        caloriesPerBase: new FormControl(null, [Validators.required, Validators.min(0)]),
        proteinsPerBase: new FormControl(null, [Validators.min(0)]),
        fatsPerBase: new FormControl(null, [Validators.min(0)]),
        carbsPerBase: new FormControl(null, [Validators.min(0)]),
        fiberPerBase: new FormControl(null, [Validators.min(0)]),
        alcoholPerBase: new FormControl(null, [Validators.min(0)]),
        visibility: new FormControl(ProductVisibility.Private, { nonNullable: true, validators: Validators.required }),
        usdaFdcId: new FormControl<number | null>(null),
    });
}

export function getDefaultProductBaseAmount(unit: MeasurementUnit): number {
    return unit === MeasurementUnit.PCS ? 1 : DEFAULT_NUTRITION_BASE_AMOUNT;
}

export function getProductControlNumberValue(control: FormControl<number | string | null>): number {
    return getControlNumericValue(control);
}

export function buildProductData(form: FormGroup<ProductFormData>, nutritionMode: NutritionMode): CreateProductRequest {
    const controls = form.controls;
    const baseAmount = getDefaultProductBaseAmount(controls.baseUnit.value);
    const defaultPortionAmount = getProductControlNumberValue(controls.defaultPortionAmount);
    const normalizeFactor = nutritionMode === 'portion' && defaultPortionAmount > 0 ? baseAmount / defaultPortionAmount : 1;
    const nutritionValues = getNormalizedNutritionValues(form, normalizeFactor);
    const imageSelection = controls.imageUrl.value;
    const productType = controls.productType.value;

    return {
        name: controls.name.value,
        barcode: controls.barcode.value,
        brand: controls.brand.value,
        productType,
        category: productType,
        description: controls.description.value,
        comment: controls.comment.value,
        imageUrl: imageSelection?.url ?? null,
        imageAssetId: imageSelection?.assetId ?? null,
        baseAmount,
        defaultPortionAmount,
        baseUnit: controls.baseUnit.value,
        ...nutritionValues,
        visibility: controls.visibility.value,
    };
}

export function buildConvertedNutritionPatch(form: FormGroup<ProductFormData>, factor: number): Partial<ProductFormValues> {
    const patch: Partial<ProductFormValues> = {};
    PRODUCT_NUTRITION_FIELDS.forEach(field => {
        const control = form.controls[field];
        const rawValue = control.value;
        if (rawValue === null) {
            return;
        }

        patch[field] = roundProductNutrientValue(getProductControlNumberValue(control) * factor);
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

export function buildAiResultPatch(form: FormGroup<ProductFormData>, result: ProductAiRecognitionResult): Partial<ProductFormValues> {
    const targetBaseAmount = getDefaultProductBaseAmount(result.baseUnit);
    const portionAmount = result.baseAmount > 0 ? result.baseAmount : targetBaseAmount;

    return {
        name: result.name.length > 0 ? result.name : form.controls.name.value,
        description: result.description ?? form.controls.description.value,
        imageUrl: result.image ?? form.controls.imageUrl.value,
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

function getNormalizedNutritionValues(form: FormGroup<ProductFormData>, normalizeFactor: number): ProductNutritionValues {
    return {
        caloriesPerBase: roundProductNutrientValue(getProductControlNumberValue(form.controls.caloriesPerBase) * normalizeFactor),
        proteinsPerBase: roundProductNutrientValue(getProductControlNumberValue(form.controls.proteinsPerBase) * normalizeFactor),
        fatsPerBase: roundProductNutrientValue(getProductControlNumberValue(form.controls.fatsPerBase) * normalizeFactor),
        carbsPerBase: roundProductNutrientValue(getProductControlNumberValue(form.controls.carbsPerBase) * normalizeFactor),
        fiberPerBase: roundProductNutrientValue(getProductControlNumberValue(form.controls.fiberPerBase) * normalizeFactor),
        alcoholPerBase: roundProductNutrientValue(getProductControlNumberValue(form.controls.alcoholPerBase) * normalizeFactor),
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
