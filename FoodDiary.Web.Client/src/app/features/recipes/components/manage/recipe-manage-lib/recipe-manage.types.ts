import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import type { Product } from '../../../../../shared/models/product.data';
import type { Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';

export type RecipeFormValues = {
    language: string;
    name: string;
    description: string | null;
    comment: string | null;
    category: string | null;
    images?: ImageSelection[];
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
    descriptionTouched?: boolean;
    images?: ImageSelection[];
    title: string | null;
    imageUrl: ImageSelection | null;
    description: string;
    ingredients: IngredientFormValues[];
};

export type IngredientFormValues = {
    textName?: string | null;
    amountText?: string | null;
    amountTouched?: boolean;
    foodNameTouched?: boolean;
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
