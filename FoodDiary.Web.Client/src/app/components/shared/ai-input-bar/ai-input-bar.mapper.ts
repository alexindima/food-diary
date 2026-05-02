import { MealManageDto } from '../../../features/meals/models/meal.data';
import { normalizeMealType, resolveMealTypeByTime } from '../../../shared/lib/meal-type.util';
import { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import { AiInputBarResult, AiInputBarResultItem } from './ai-input-bar.types';

export function mapNutritionItemsToAiInputBarItems(nutrition: FoodNutritionResponse, matches: FoodVisionItem[]): AiInputBarResultItem[] {
    return (
        nutrition.items?.map(item => {
            const normalizedName = (item.name ?? '').trim().toLowerCase();
            const match = matches.find(
                result =>
                    result.nameEn?.trim().toLowerCase() === normalizedName || result.nameLocal?.trim().toLowerCase() === normalizedName,
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
        }) ?? []
    );
}

export function buildMealManageDtoFromAiResult(result: AiInputBarResult, mealDate?: Date): MealManageDto {
    const resolvedMealDate = mealDate ?? (result.date && result.time ? new Date(`${result.date}T${result.time}`) : new Date());
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
    if (!value) {
        return 3;
    }

    if (value > 5) {
        return Math.min(5, Math.max(1, Math.round(value / 2)));
    }

    return Math.max(1, value);
}
