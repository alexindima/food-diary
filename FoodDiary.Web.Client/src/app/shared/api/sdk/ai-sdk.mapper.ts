import type { FoodNutritionResponse, FoodVisionResponse, ProductLabel } from '../../models/ai.data';
import type { FoodRecognitionJob } from '../../models/food-recognition.data';
import type { RecipeImportResult } from '../../models/recipe-import.data';
import { utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
import { publicImageUrl } from '../../models/semantics/image-location';
import type { FoodNutritionHttpResponse } from './generated/model/food-nutrition-http-response';
import type { FoodRecognitionJobHttpResponse } from './generated/model/food-recognition-job-http-response';
import type { FoodVisionHttpResponse } from './generated/model/food-vision-http-response';
import type { ProductLabelHttpResponse } from './generated/model/product-label-http-response';
import type { RecipeImportHttpResponse } from './generated/model/recipe-import-http-response';
import { requireSdkFields, sdkEnum, sdkMaybe, sdkNullableFields, sdkOptional } from './sdk-response';

function productLabelFromSdk(value: ProductLabelHttpResponse): ProductLabel {
    return {
        ...sdkNullableFields(value, ['name', 'brand', 'baseAmount', 'calories', 'protein', 'fat', 'carbs', 'fiber', 'alcohol', 'notes']),
        baseUnit: sdkOptional(value.baseUnit, unit => sdkEnum(unit, ['g', 'ml', 'pcs'] as const)),
    };
}

export function foodVisionFromSdk(response: FoodVisionHttpResponse): FoodVisionResponse {
    const value = requireSdkFields(response, ['items']);
    return {
        ...value,
        items: value.items.map(item => requireSdkFields(item, ['nameEn', 'amount', 'unit', 'confidence'])),
        productLabel: sdkMaybe(value.productLabel, productLabelFromSdk),
    };
}

export function foodNutritionFromSdk(response: FoodNutritionHttpResponse): FoodNutritionResponse {
    const value = requireSdkFields(response, ['calories', 'protein', 'fat', 'carbs', 'fiber', 'alcohol', 'items']);
    return {
        ...value,
        items: value.items.map(item =>
            requireSdkFields(item, ['name', 'amount', 'unit', 'calories', 'protein', 'fat', 'carbs', 'fiber', 'alcohol']),
        ),
    };
}

export function recognitionJobFromSdk(response: FoodRecognitionJobHttpResponse): FoodRecognitionJob {
    const value = requireSdkFields(response, ['id', 'imageAssetId', 'imageUrl', 'status', 'createdOnUtc', 'updatedOnUtc']);
    return {
        ...sdkNullableFields(value, ['description', 'errorCode', 'nutritionErrorCode']),
        id: entityId<'food-recognition'>(value.id),
        imageAssetId: entityId<'image-asset'>(value.imageAssetId),
        imageUrl: publicImageUrl(value.imageUrl),
        createdOnUtc: utcInstant(value.createdOnUtc),
        updatedOnUtc: utcInstant(value.updatedOnUtc),
        status: sdkEnum(value.status, ['Queued', 'Running', 'Succeeded', 'Failed'] as const),
        additionalImages: value.additionalImages?.map(responseImage => {
            const image = requireSdkFields(responseImage, ['imageAssetId', 'imageUrl']);
            return { ...image, imageAssetId: entityId<'image-asset'>(image.imageAssetId), imageUrl: publicImageUrl(image.imageUrl) };
        }),
        vision: sdkOptional(value.vision, foodVisionFromSdk),
        nutrition: sdkOptional(value.nutrition, foodNutritionFromSdk),
    };
}

export function recipeImportFromSdk(response: RecipeImportHttpResponse): RecipeImportResult {
    const value = requireSdkFields(response, ['name', 'ingredients', 'steps']);
    return {
        ...sdkNullableFields(value, ['description', 'servings', 'prepMinutes', 'cookMinutes', 'authorNutrition', 'sourceUrl']),
        ingredients: value.ingredients.map(item => sdkNullableFields(requireSdkFields(item, ['name']), ['amount'])),
    };
}
