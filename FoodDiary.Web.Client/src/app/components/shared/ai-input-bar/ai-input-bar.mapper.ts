import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import type { ImageSelection } from '../../../shared/models/image-upload.data';
import type { AiInputBarMealDetails, AiInputBarResult, AiInputBarResultItem, AiRecognitionSource } from './ai-input-bar.types';

export function mapNutritionItemsToAiInputBarItems(nutrition: FoodNutritionResponse, matches: FoodVisionItem[]): AiInputBarResultItem[] {
    return nutrition.items.map(item => {
        const normalizedName = item.name.trim().toLowerCase();
        const match = matches.find(
            result => result.nameEn.trim().toLowerCase() === normalizedName || result.nameLocal?.trim().toLowerCase() === normalizedName,
        );

        return {
            nameEn: match?.nameEn ?? item.name,
            nameLocal: match?.nameLocal ?? null,
            amount: item.amount,
            unit: item.unit,
            calories: item.calories,
            proteins: item.protein,
            fats: item.fat,
            carbs: item.carbs,
            fiber: item.fiber,
            alcohol: item.alcohol,
        };
    });
}

export function buildTextAiInputBarResult(params: {
    source: AiRecognitionSource;
    mealType: string | null;
    recognizedAtUtc: string;
    query: string | null;
    details: AiInputBarMealDetails;
    nutrition: FoodNutritionResponse;
    results: FoodVisionItem[];
}): AiInputBarResult {
    return {
        source: params.source,
        mealType: params.mealType,
        recognizedAtUtc: params.recognizedAtUtc,
        notes: params.query,
        date: params.details.date,
        time: params.details.time,
        comment: params.details.comment ?? null,
        preMealSatietyLevel: params.details.preMealSatietyLevel ?? null,
        postMealSatietyLevel: params.details.postMealSatietyLevel ?? null,
        items: mapNutritionItemsToAiInputBarItems(params.nutrition, params.results),
    };
}

export function buildPhotoAiInputBarResult(params: {
    mealType: string | null;
    recognizedAtUtc: string;
    selection: ImageSelection | null;
    details: AiInputBarMealDetails;
    nutrition: FoodNutritionResponse;
    results: FoodVisionItem[];
}): AiInputBarResult {
    return {
        source: 'Photo',
        mealType: params.mealType,
        imageAssetId: params.selection?.assetId ?? null,
        imageUrl: params.selection?.url ?? null,
        recognizedAtUtc: params.recognizedAtUtc,
        notes: params.nutrition.notes ?? null,
        date: params.details.date,
        time: params.details.time,
        comment: params.details.comment ?? null,
        preMealSatietyLevel: params.details.preMealSatietyLevel ?? null,
        postMealSatietyLevel: params.details.postMealSatietyLevel ?? null,
        items: mapNutritionItemsToAiInputBarItems(params.nutrition, params.results),
    };
}
