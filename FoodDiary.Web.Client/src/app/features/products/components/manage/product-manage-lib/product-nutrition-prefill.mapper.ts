import { DEFAULT_NUTRITION_BASE_AMOUNT, KJ_TO_KCAL_FACTOR } from '../../../../../shared/lib/nutrition.constants';
import { USDA_NUTRIENT_IDS } from '../../../../usda/lib/usda-nutrient.constants';
import type { Micronutrient, UsdaFoodDetail } from '../../../../usda/models/usda.data';
import type { OpenFoodFactsProduct } from '../../../api/open-food-facts.service';
import { MeasurementUnit, type ProductSearchSuggestion } from '../../../models/product.data';
import { buildResetNutritionPatch, roundProductNutrientValue } from './product-manage-form.mapper';
import type { ProductFormValues } from './product-manage-form.types';

type NutritionSourceProduct = OpenFoodFactsProduct | ProductSearchSuggestion;
type ProductNutritionPatchField = keyof Pick<
    ProductFormValues,
    'caloriesPerBase' | 'proteinsPerBase' | 'fatsPerBase' | 'carbsPerBase' | 'fiberPerBase' | 'alcoholPerBase'
>;

export function buildOpenFoodFactsLookupPatch(values: ProductFormValues, offProduct: OpenFoodFactsProduct): Partial<ProductFormValues> {
    const patch: Partial<ProductFormValues> = {};

    if (values.name.length === 0) {
        patch.name = offProduct.name;
    }
    if (isEmpty(values.brand) && hasText(offProduct.brand)) {
        patch.brand = offProduct.brand;
    }

    applyNutritionSourcePatch(patch, values, offProduct, false);
    return patch;
}

export function buildSourceProductPrefillPatch(product: NutritionSourceProduct): Partial<ProductFormValues> {
    const patch: Partial<ProductFormValues> = {};

    if (hasText(product.barcode)) {
        patch.barcode = product.barcode;
    }
    if (product.name.length > 0) {
        patch.name = product.name;
    }
    if (hasText(product.brand)) {
        patch.brand = product.brand;
    }

    applyNutritionSourcePatch(patch, null, product, true);
    return patch;
}

export function buildUsdaFoodDetailPrefillPatch(detail: UsdaFoodDetail): Partial<ProductFormValues> {
    const nutrients = detail.nutrients;
    const calories = getUsdaNutrientAmount(nutrients, [USDA_NUTRIENT_IDS.energy], [/^energy$/i]);
    const proteins = getUsdaNutrientAmount(nutrients, [USDA_NUTRIENT_IDS.protein], [/^protein$/i]);
    const fats = getUsdaNutrientAmount(nutrients, [USDA_NUTRIENT_IDS.fat], [/total lipid/i, /^fat$/i]);
    const carbs = getUsdaNutrientAmount(nutrients, [USDA_NUTRIENT_IDS.carbs], [/carbohydrate/i]);
    const fiber = getUsdaNutrientAmount(nutrients, [USDA_NUTRIENT_IDS.fiber], [/fiber/i]);
    const alcohol = getUsdaNutrientAmount(nutrients, [USDA_NUTRIENT_IDS.alcohol], [/alcohol/i]);

    return {
        name: detail.description,
        usdaFdcId: detail.fdcId,
        baseUnit: MeasurementUnit.G,
        baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        ...(calories === null ? {} : { caloriesPerBase: Math.round(calories) }),
        ...(proteins === null ? {} : { proteinsPerBase: roundProductNutrientValue(proteins) }),
        ...(fats === null ? {} : { fatsPerBase: roundProductNutrientValue(fats) }),
        ...(carbs === null ? {} : { carbsPerBase: roundProductNutrientValue(carbs) }),
        ...(fiber === null ? {} : { fiberPerBase: roundProductNutrientValue(fiber) }),
        ...(alcohol === null ? {} : { alcoholPerBase: roundProductNutrientValue(alcohol) }),
    };
}

export { buildResetNutritionPatch };

function applyNutritionSourcePatch(
    patch: Partial<ProductFormValues>,
    values: ProductFormValues | null,
    product: NutritionSourceProduct,
    overwrite: boolean,
): void {
    const context = { patch, values, overwrite };
    setNutritionPatchValue(context, 'caloriesPerBase', product.caloriesPer100G, true);
    setNutritionPatchValue(context, 'proteinsPerBase', product.proteinsPer100G);
    setNutritionPatchValue(context, 'fatsPerBase', product.fatsPer100G);
    setNutritionPatchValue(context, 'carbsPerBase', product.carbsPer100G);
    setNutritionPatchValue(context, 'fiberPerBase', product.fiberPer100G);
}

function setNutritionPatchValue(
    context: {
        patch: Partial<ProductFormValues>;
        values: ProductFormValues | null;
        overwrite: boolean;
    },
    field: ProductNutritionPatchField,
    value: number | null | undefined,
    whole = false,
): void {
    if (value === null || value === undefined) {
        return;
    }

    if (!context.overwrite && context.values?.[field] !== null) {
        return;
    }

    context.patch[field] = whole ? Math.round(value) : roundProductNutrientValue(value);
}

function getUsdaNutrientAmount(nutrients: Micronutrient[], nutrientIds: number[], namePatterns: RegExp[]): number | null {
    const nutrient =
        nutrients.find(item => nutrientIds.includes(item.nutrientId)) ??
        nutrients.find(item => namePatterns.some(pattern => pattern.test(item.name)));

    if (nutrient === undefined) {
        return null;
    }

    return nutrient.unit.toLowerCase() === 'kj' ? nutrient.amountPer100g * KJ_TO_KCAL_FACTOR : nutrient.amountPer100g;
}

function hasText(value: string | null | undefined): value is string {
    return value !== null && value !== undefined && value.length > 0;
}

function isEmpty(value: string | null): boolean {
    return value === null || value.length === 0;
}
