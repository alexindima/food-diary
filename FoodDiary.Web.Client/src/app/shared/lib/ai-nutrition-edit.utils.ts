import type { FoodNutritionResponse } from '../models/ai.data';

export type AiNutritionEditableItem = {
    id: string;
    name: string;
    nameEn?: string | null;
    amount: number;
    unit: string;
};

type NutritionTotals = Pick<FoodNutritionResponse, 'calories' | 'protein' | 'fat' | 'carbs' | 'fiber' | 'alcohol'>;

const EMPTY_NUTRITION_TOTALS: NutritionTotals = {
    calories: 0,
    protein: 0,
    fat: 0,
    carbs: 0,
    fiber: 0,
    alcohol: 0,
};

export function normalizeAiNutritionItemName(value?: string | null): string {
    return (value ?? '').trim().toLowerCase();
}

export function recalculateEditedAiNutrition(
    nutrition: FoodNutritionResponse | null,
    sourceItems: readonly AiNutritionEditableItem[],
    editedItems: readonly AiNutritionEditableItem[],
): FoodNutritionResponse | null {
    if (nutrition === null) {
        return null;
    }

    const nutritionByName = new Map(nutrition.items.map(item => [normalizeAiNutritionItemName(item.name), item]));

    const updatedItems = editedItems
        .map(item => {
            const base = sourceItems.find(sourceItem => sourceItem.id === item.id);
            const originalName = base?.nameEn ?? base?.name ?? item.name;
            const originalNutrition = nutritionByName.get(normalizeAiNutritionItemName(originalName));
            if (originalNutrition === undefined) {
                return null;
            }

            const ratio = base !== undefined && base.amount > 0 ? item.amount / base.amount : 1;
            return {
                name: item.name,
                amount: item.amount,
                unit: item.unit,
                calories: originalNutrition.calories * ratio,
                protein: originalNutrition.protein * ratio,
                fat: originalNutrition.fat * ratio,
                carbs: originalNutrition.carbs * ratio,
                fiber: originalNutrition.fiber * ratio,
                alcohol: originalNutrition.alcohol * ratio,
            };
        })
        .filter((item): item is FoodNutritionResponse['items'][number] => item !== null);

    if (updatedItems.length !== editedItems.length) {
        return null;
    }

    const totals = updatedItems.reduce<NutritionTotals>(
        (acc, item) => ({
            calories: acc.calories + item.calories,
            protein: acc.protein + item.protein,
            fat: acc.fat + item.fat,
            carbs: acc.carbs + item.carbs,
            fiber: acc.fiber + item.fiber,
            alcohol: acc.alcohol + item.alcohol,
        }),
        { ...EMPTY_NUTRITION_TOTALS },
    );

    return { ...totals, items: updatedItems, notes: nutrition.notes ?? null };
}
