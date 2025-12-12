import { Consumption, ConsumptionSourceType } from '../types/consumption.data';

const MEAL_STUBS: Record<string, string> = {
    BREAKFAST: 'assets/images/stubs/meals/breakfast.svg',
    LUNCH: 'assets/images/stubs/meals/lunch.svg',
    DINNER: 'assets/images/stubs/meals/dinner.svg',
    SNACK: 'assets/images/stubs/meals/snack.svg',
    OTHER: 'assets/images/stubs/meals/other.svg',
};

export function resolveMealImageUrl(imageUrl: string | null | undefined, mealType: string | null | undefined): string | undefined {
    if (imageUrl && imageUrl.trim().length > 0) {
        return imageUrl;
    }
    if (!mealType) {
        return MEAL_STUBS['OTHER'];
    }

    const key = mealType.toUpperCase();
    return MEAL_STUBS[key] ?? MEAL_STUBS['OTHER'];
}
