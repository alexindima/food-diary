import type { AiInputBarResult } from '../../../components/shared/ai-input-bar/ai-input-bar.types';
import { normalizeMealType, resolveMealTypeByTime } from '../../../shared/lib/meal-type.util';
import { normalizeSatietyLevel } from '../../../shared/lib/satiety-level.utils';
import type { MealManageDto } from '../models/meal.data';

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
