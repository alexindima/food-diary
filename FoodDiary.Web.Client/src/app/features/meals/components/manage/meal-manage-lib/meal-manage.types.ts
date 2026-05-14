import type { FormGroupControls } from '../../../../../shared/lib/common.data';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { Product } from '../../../../products/models/product.data';
import type { Recipe } from '../../../../recipes/models/recipe.data';
import type { ConsumptionSourceType } from '../../../models/meal.data';

export type ConsumptionFormValues = {
    date: string;
    time: string;
    mealType: string | null;
    items: ConsumptionItemFormValues[];
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

export type ConsumptionItemFormValues = {
    sourceType: ConsumptionSourceType;
    product: Product | null;
    recipe: Recipe | null;
    amount: number | null;
};

export type ConsumptionFormData = FormGroupControls<ConsumptionFormValues>;

export type ConsumptionItemFormData = FormGroupControls<ConsumptionItemFormValues>;

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
