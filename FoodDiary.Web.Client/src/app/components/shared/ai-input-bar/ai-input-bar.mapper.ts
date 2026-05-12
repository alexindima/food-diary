import type { FoodNutritionResponse, FoodVisionItem } from '../../../shared/models/ai.data';
import type { AiInputBarResultItem } from './ai-input-bar.types';

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
