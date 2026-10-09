import type { MealPlanDayId, MealPlanId, MealPlanMealId, RecipeId } from '../../../shared/models/semantics/entity-id';
import type { PlanDayNumber, PlanDurationDays, PlannedMealType, PlannedServings } from './meal-plan-values';
export type MealPlanSummary = {
    id: MealPlanId;
    name: string;
    description?: string | null;
    dietType: DietType;
    durationDays: PlanDurationDays;
    targetCaloriesPerDay?: number | null;
    isCurated: boolean;
    totalRecipes: number;
};

export type MealPlan = {
    id: MealPlanId;
    name: string;
    description?: string | null;
    dietType: DietType;
    durationDays: PlanDurationDays;
    targetCaloriesPerDay?: number | null;
    isCurated: boolean;
    days: MealPlanDay[];
};

export type MealPlanDay = {
    id: MealPlanDayId;
    dayNumber: PlanDayNumber;
    meals: MealPlanMeal[];
};

export type MealPlanMeal = {
    id: MealPlanMealId;
    mealType: PlannedMealType;
    recipeId: RecipeId;
    recipeName?: string | null;
    servings: PlannedServings;
    calories?: number | null;
    proteins?: number | null;
    fats?: number | null;
    carbs?: number | null;
};

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
