import { MealManageDto } from '../../../features/meals/models/meal.data';
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

    return {
        date: resolvedMealDate,
        comment: result.comment ?? undefined,
        isNutritionAutoCalculated: false,
        manualCalories: sumItems(result.items, item => item.calories),
        manualProteins: sumItems(result.items, item => item.proteins),
        manualFats: sumItems(result.items, item => item.fats),
        manualCarbs: sumItems(result.items, item => item.carbs),
        manualFiber: sumItems(result.items, item => item.fiber),
        manualAlcohol: sumItems(result.items, item => item.alcohol),
        preMealSatietyLevel: result.preMealSatietyLevel ?? undefined,
        postMealSatietyLevel: result.postMealSatietyLevel ?? undefined,
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

function sumItems(items: AiInputBarResultItem[], selector: (item: AiInputBarResultItem) => number): number {
    return items.reduce((sum, item) => sum + selector(item), 0);
}
