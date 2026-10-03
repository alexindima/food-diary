import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { MealSourceType } from '../../../../../shared/models/meal.data';
import type { Product } from '../../../../../shared/models/product.data';
import type { Recipe } from '../../../../../shared/models/recipe.data';

export type MealFormValues = {
    date: string;
    time: string;
    mealType: string | null;
    items: MealItemFormValues[];
    comment: string | null;
    imageUrl: ImageSelection | null;
    isNutritionAutoCalculated: boolean;
    manualCalories: number | null;
    manualProteins: number | null;
    manualFats: number | null;
    manualCarbs: number | null;
    manualFiber: number | null;
    manualAlcohol: number | null;
    preMealSatietyLevel: number | null;
    postMealSatietyLevel: number | null;
};

export type MealItemFormValues = {
    sourceType: MealSourceType;
    product: Product | null;
    recipe: Recipe | null;
    amount: number | null;
};

export type NutritionTotals = {
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
};

export type NutritionMode = 'auto' | 'manual';
export type MacroKey = 'proteins' | 'fats' | 'carbs';

export type MacroBarSegment = {
    key: MacroKey;
    percent: number;
};

export type MacroBarState = {
    isEmpty: boolean;
    segments: MacroBarSegment[];
};

export type CalorieMismatchWarning = {
    expectedCalories: number;
    actualCalories: number;
};

export type MealNutritionSummaryState = {
    autoTotals: NutritionTotals;
    summaryTotals: NutritionTotals;
    warning: CalorieMismatchWarning | null;
};
