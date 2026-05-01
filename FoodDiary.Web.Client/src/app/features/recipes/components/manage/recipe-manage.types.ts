import { FormGroupControls } from '../../../../shared/lib/common.data';
import { ImageSelection } from '../../../../shared/models/image-upload.data';
import { Product } from '../../../products/models/product.data';
import { RecipeVisibility } from '../../models/recipe.data';

export interface RecipeFormValues {
    name: string;
    description: string | null;
    comment: string | null;
    category: string | null;
    imageUrl: ImageSelection | null;
    prepTime: number | null;
    cookTime: number | null;
    servings: number;
    visibility: RecipeVisibility;
    calculateNutritionAutomatically: boolean;
    manualCalories: number | null;
    manualProteins: number | null;
    manualFats: number | null;
    manualCarbs: number | null;
    manualFiber: number | null;
    manualAlcohol: number | null;
    steps: StepFormValues[];
}

export interface StepFormValues {
    title: string | null;
    imageUrl: ImageSelection | null;
    description: string;
    ingredients: IngredientFormValues[];
}

export interface IngredientFormValues {
    food: Product | null;
    amount: number | null;
    foodName: string | null;
    nestedRecipeId: string | null;
    nestedRecipeName: string | null;
}

export type RecipeFormData = FormGroupControls<RecipeFormValues>;
export type StepFormData = FormGroupControls<StepFormValues>;
export type IngredientFormData = FormGroupControls<IngredientFormValues>;

export interface CalorieMismatchWarning {
    expectedCalories: number;
    actualCalories: number;
}

export type NutritionMode = 'auto' | 'manual';
export type NutritionScaleMode = 'recipe' | 'portion';
export type MacroKey = 'proteins' | 'fats' | 'carbs';

export interface MacroBarSegment {
    key: MacroKey;
    percent: number;
}

export interface MacroBarState {
    isEmpty: boolean;
    segments: MacroBarSegment[];
}
