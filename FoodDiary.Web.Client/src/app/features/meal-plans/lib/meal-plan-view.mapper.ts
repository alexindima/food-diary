import {
    DIET_TYPES,
    type DietType,
    type MealPlan,
    type MealPlanDay,
    type MealPlanMeal,
    type MealPlanSummary,
} from '../models/meal-plan.data';

export type MealPlanCardViewModel = {
    dietTypeKey: string;
} & MealPlanSummary;

export type MealPlanDietFilterViewModel = {
    value: DietType | null;
    labelKey: string;
    fill: 'solid' | 'outline';
};

export type MealPlanDetailViewModel = {
    header: MealPlanDetailHeaderViewModel;
    days: MealPlanDayViewModel[];
};

export type MealPlanDetailHeaderViewModel = {
    dietTypeKey: string;
    name: string;
    description?: string | null;
    isCurated: boolean;
};

export type MealPlanDayViewModel = {
    meals: MealPlanMealViewModel[];
} & Omit<MealPlanDay, 'meals'>;

export type MealPlanMealViewModel = {
    mealTypeKey: string;
    nutritionItems: MealPlanNutritionItem[];
} & MealPlanMeal;

export type MealPlanNutritionItem = {
    unitKey: string;
    value: number;
    prefix: string;
};

const ALL_DIETS_FILTER_OPTION: Omit<MealPlanDietFilterViewModel, 'fill'> = {
    value: null,
    labelKey: 'MEAL_PLANS.FILTER_ALL',
};

export function buildMealPlanDietFilterOptions(selectedType: DietType | null): MealPlanDietFilterViewModel[] {
    return [
        ALL_DIETS_FILTER_OPTION,
        ...DIET_TYPES.map(type => ({
            value: type.value,
            labelKey: type.labelKey,
        })),
    ].map(type => ({
        ...type,
        fill: selectedType === type.value ? 'solid' : 'outline',
    }));
}

export function buildMealPlanCards(plans: MealPlanSummary[]): MealPlanCardViewModel[] {
    return plans.map(plan => ({
        ...plan,
        dietTypeKey: buildDietTypeTranslationKey(plan.dietType),
    }));
}

export function buildMealPlanDetailView(plan: MealPlan | null): MealPlanDetailViewModel | null {
    if (plan === null) {
        return null;
    }

    return {
        header: {
            dietTypeKey: buildDietTypeTranslationKey(plan.dietType),
            name: plan.name,
            description: plan.description,
            isCurated: plan.isCurated,
        },
        days: plan.days.map(day => ({
            ...day,
            meals: day.meals.map(meal => ({
                ...meal,
                mealTypeKey: `MEAL_PLANS.MEAL_TYPE.${meal.mealType.toUpperCase()}`,
                nutritionItems: buildMealNutritionItems(meal),
            })),
        })),
    };
}

function buildDietTypeTranslationKey(dietType: DietType): string {
    return `MEAL_PLANS.DIET_TYPE.${dietType.toUpperCase()}`;
}

function buildMealNutritionItems(meal: MealPlanMeal): MealPlanNutritionItem[] {
    return [
        { unitKey: 'GENERAL.UNITS.KCAL', value: meal.calories, prefix: '' },
        { unitKey: 'GENERAL.UNITS.G', value: meal.proteins, prefix: 'P: ' },
        { unitKey: 'GENERAL.UNITS.G', value: meal.fats, prefix: 'F: ' },
        { unitKey: 'GENERAL.UNITS.G', value: meal.carbs, prefix: 'C: ' },
    ].filter((item): item is MealPlanNutritionItem => item.value !== null && item.value !== undefined && item.value > 0);
}
