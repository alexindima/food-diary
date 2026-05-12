export interface MealPlanSummary {
    id: string;
    name: string;
    description?: string | null;
    dietType: DietType;
    durationDays: number;
    targetCaloriesPerDay?: number | null;
    isCurated: boolean;
    totalRecipes: number;
}

export interface MealPlan {
    id: string;
    name: string;
    description?: string | null;
    dietType: DietType;
    durationDays: number;
    targetCaloriesPerDay?: number | null;
    isCurated: boolean;
    days: MealPlanDay[];
}

export interface MealPlanDay {
    id: string;
    dayNumber: number;
    meals: MealPlanMeal[];
}

export interface MealPlanMeal {
    id: string;
    mealType: string;
    recipeId: string;
    recipeName?: string | null;
    servings: number;
    calories?: number | null;
    proteins?: number | null;
    fats?: number | null;
    carbs?: number | null;
}

export type DietType = 'Balanced' | 'HighProtein' | 'LowCarb' | 'Keto' | 'Mediterranean' | 'Vegan' | 'Vegetarian';

export const DIET_TYPES: Array<{ value: DietType; labelKey: string }> = [
    { value: 'Balanced', labelKey: 'MEAL_PLANS.DIET_TYPE.BALANCED' },
    { value: 'HighProtein', labelKey: 'MEAL_PLANS.DIET_TYPE.HIGH_PROTEIN' },
    { value: 'LowCarb', labelKey: 'MEAL_PLANS.DIET_TYPE.LOW_CARB' },
    { value: 'Keto', labelKey: 'MEAL_PLANS.DIET_TYPE.KETO' },
    { value: 'Mediterranean', labelKey: 'MEAL_PLANS.DIET_TYPE.MEDITERRANEAN' },
    { value: 'Vegan', labelKey: 'MEAL_PLANS.DIET_TYPE.VEGAN' },
    { value: 'Vegetarian', labelKey: 'MEAL_PLANS.DIET_TYPE.VEGETARIAN' },
];
