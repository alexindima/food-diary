import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { Product } from '../../../../products/models/product.data';
import type { Recipe, RecipeVisibility } from '../../../models/recipe.data';

export type RecipeFormValues = {
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
};

export type StepFormValues = {
    title: string | null;
    imageUrl: ImageSelection | null;
    description: string;
    ingredients: IngredientFormValues[];
};

export type IngredientFormValues = {
    food: Product | null;
    productId: string | null;
    amount: number | null;
    foodName: string | null;
    nestedRecipe: Recipe | null;
    nestedRecipeId: string | null;
    nestedRecipeName: string | null;
};

export type NutritionMode = 'auto' | 'manual';
export type NutritionScaleMode = 'recipe' | 'portion';
