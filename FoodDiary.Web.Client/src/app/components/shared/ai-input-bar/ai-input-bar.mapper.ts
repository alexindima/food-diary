import type { MealManageDto } from '../../../features/meals/models/meal.data';
import { normalizeMealType, resolveMealTypeByTime } from '../../../shared/lib/meal-type.util';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import type { AiInputBarResult, AiInputBarResultItem } from './ai-input-bar.types';

const DEFAULT_SATIETY_LEVEL = 3;
const MAX_SATIETY_LEVEL = 5;
const MIN_SATIETY_LEVEL = 1;
const AI_SATIETY_SCALE_FACTOR = 2;

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

export function buildMealManageDtoFromAiResult(result: AiInputBarResult, mealDate?: Date): MealManageDto {
    const resultDate = result.date ?? '';
    const resultTime = result.time ?? '';
    const hasResultDateTime = resultDate.length > 0 && resultTime.length > 0;
    const resolvedMealDate = mealDate ?? (hasResultDateTime ? new Date(`${resultDate}T${resultTime}`) : new Date());
    const resolvedMealType = normalizeMealType(result.mealType) ?? resolveMealTypeByTime(resolvedMealDate);

    return {
        date: resolvedMealDate,
        mealType: resolvedMealType,
        comment: result.comment ?? undefined,
        imageAssetId: result.imageAssetId ?? undefined,
        imageUrl: result.imageUrl ?? undefined,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: normalizeSatietyLevel(result.preMealSatietyLevel),
        postMealSatietyLevel: normalizeSatietyLevel(result.postMealSatietyLevel),
        items: [],
        aiSessions: [
            {
                source: result.source,
                imageAssetId: result.imageAssetId,
                imageUrl: result.imageUrl,
                recognizedAtUtc: result.recognizedAtUtc,
                notes: result.notes,
                items: result.items,
            },
        ],
    };
}

function normalizeSatietyLevel(value: number | null | undefined): number {
    if (value === null || value === undefined || Number.isNaN(value)) {
        return DEFAULT_SATIETY_LEVEL;
    }

    if (value > MAX_SATIETY_LEVEL) {
        return Math.min(MAX_SATIETY_LEVEL, Math.max(MIN_SATIETY_LEVEL, Math.round(value / AI_SATIETY_SCALE_FACTOR)));
    }

    return Math.max(MIN_SATIETY_LEVEL, value);
}
