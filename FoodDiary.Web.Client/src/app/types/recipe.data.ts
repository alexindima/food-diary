import { NutrientData } from './charts.data';
import { MeasurementUnit, Product } from './product.data';

export enum RecipeVisibility {
    Private = 'Private',
    Public = 'Public',
}

export interface Recipe {
    id: string;
    name: string;
    description?: string | null;
    comment?: string | null;
    category?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    prepTime?: number | null;
    cookTime?: number | null;
    servings: number;
    visibility: RecipeVisibility;
    usageCount: number;
    createdAt: string;
    isOwnedByCurrentUser: boolean;
    totalCalories?: number | null;
    totalProteins?: number | null;
    totalFats?: number | null;
    totalCarbs?: number | null;
    totalFiber?: number | null;
    totalAlcohol?: number | null;
    isNutritionAutoCalculated: boolean;
    manualCalories?: number | null;
    manualProteins?: number | null;
    manualFats?: number | null;
    manualCarbs?: number | null;
    manualFiber?: number | null;
    manualAlcohol?: number | null;
    steps: RecipeStep[];
    nutrientChartData?: NutrientData;
}

export interface RecipeStep {
    id: string;
    stepNumber: number;
    title?: string | null;
    instruction: string;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    ingredients: RecipeIngredient[];
}

export interface RecipeIngredient {
    id: string;
    amount: number;
    productId?: string | null;
    productName?: string | null;
    productBaseUnit?: MeasurementUnit | string | null;
    productBaseAmount?: number | null;
    productCaloriesPerBase?: number | null;
    productProteinsPerBase?: number | null;
    productFatsPerBase?: number | null;
    productCarbsPerBase?: number | null;
    productFiberPerBase?: number | null;
    productAlcoholPerBase?: number | null;
    nestedRecipeId?: string | null;
    nestedRecipeName?: string | null;
}

export interface RecipeFilters {
    search?: string | null;
}

export interface RecipeDto {
    name: string;
    description?: string | null;
    comment?: string | null;
    category?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    prepTime: number;
    cookTime: number;
    servings: number;
    visibility: RecipeVisibility;
    calculateNutritionAutomatically: boolean;
    manualCalories?: number | null;
    manualProteins?: number | null;
    manualFats?: number | null;
    manualCarbs?: number | null;
    manualFiber?: number | null;
    manualAlcohol?: number | null;
    steps: RecipeStepDto[];
}

export interface RecipeStepDto {
    title?: string | null;
    description: string;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    ingredients: RecipeIngredientDto[];
}

export interface RecipeIngredientDto {
    productId?: string;
    nestedRecipeId?: string;
    amount: number;
}
