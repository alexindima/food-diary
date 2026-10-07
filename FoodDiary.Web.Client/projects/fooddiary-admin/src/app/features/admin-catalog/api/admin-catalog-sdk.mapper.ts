import type { CatalogProductHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-product-http-response';
import type { CatalogProductImportHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-product-import-http-response';
import type { CatalogRecipeExportHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-recipe-export-http-response';
import type { CatalogRecipeImportHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-recipe-import-http-response';
import type { CatalogRecipeIngredientHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-recipe-ingredient-http-response';
import type { CatalogRecipeStepHttpResponse } from '../../../shared/api/sdk/generated/model/catalog-recipe-step-http-response';
import { requireSdkFields, sdkEnum, sdkNullableFields } from '../../../shared/api/sdk/sdk-response';
import type { CatalogIngredient, CatalogProduct, CatalogRecipe, CatalogResult, CatalogStep } from '../models/catalog-transfer';

export function catalogProductFromSdk(response: CatalogProductHttpResponse): CatalogProduct {
    const value = requireSdkFields(response, [
        'id',
        'name',
        'productType',
        'baseUnit',
        'baseAmount',
        'defaultPortionAmount',
        'caloriesPerBase',
        'proteinsPerBase',
        'fatsPerBase',
        'carbsPerBase',
        'fiberPerBase',
        'alcoholPerBase',
    ]);
    return {
        ...value,
        barcode: value.barcode ?? null,
        brand: value.brand ?? null,
        category: value.category ?? null,
        description: value.description ?? null,
        imageUrl: value.imageUrl ?? null,
    };
}

export function catalogRecipeExportFromSdk(response: CatalogRecipeExportHttpResponse): CatalogRecipe {
    const normalized = sdkNullableFields(response, [
        'description',
        'category',
        'imageUrl',
        'prepTime',
        'cookTime',
        'manualCalories',
        'manualProteins',
        'manualFats',
        'manualCarbs',
        'manualFiber',
        'manualAlcohol',
    ]);
    const value = requireSdkFields(normalized, [
        'id',
        'name',
        'servings',
        'language',
        'languageConfirmed',
        'calculateNutritionAutomatically',
        'steps',
    ]);
    return {
        ...value,
        steps: value.steps.map(item => catalogRecipeStepFromSdk(item)),
    };
}

export function catalogRecipeStepFromSdk(response: CatalogRecipeStepHttpResponse): CatalogStep {
    const value = requireSdkFields(response, ['order', 'description', 'ingredients']);
    return {
        ...value,
        title: value.title ?? null,
        imageUrl: value.imageUrl ?? null,
        ingredients: value.ingredients.map(item => catalogRecipeIngredientFromSdk(item)),
    };
}

export function catalogRecipeIngredientFromSdk(response: CatalogRecipeIngredientHttpResponse): CatalogIngredient {
    const value = requireSdkFields(response, ['amount']);
    return { ...value, productId: value.productId ?? null, nestedRecipeId: value.nestedRecipeId ?? null };
}

export function catalogProductImportFromSdk(response: CatalogProductImportHttpResponse): CatalogResult {
    const value = requireSdkFields(response, ['id', 'status', 'errors']);
    return { ...value, status: sdkEnum(value.status, ['ready', 'skipped', 'invalid', 'imported', 'failed'] as const) };
}

export function catalogRecipeImportFromSdk(response: CatalogRecipeImportHttpResponse): CatalogResult {
    const value = requireSdkFields(response, ['id', 'status', 'errors']);
    return { ...value, status: sdkEnum(value.status, ['ready', 'skipped', 'invalid', 'imported', 'failed'] as const) };
}
