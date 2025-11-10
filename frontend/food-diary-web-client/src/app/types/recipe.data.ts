import { NutrientChartData } from './charts.data';
import { MeasurementUnit, Product } from './product.data';

export enum RecipeVisibility {
    Private = 'Private',
    Public = 'Public',
}

export interface Recipe {
    id: string;
    name: string;
    description?: string | null;
    category?: string | null;
    imageUrl?: string | null;
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
    steps: RecipeStep[];
    nutrientChartData?: NutrientChartData;
}

export interface RecipeStep {
    id: string;
    stepNumber: number;
    instruction: string;
    imageUrl?: string | null;
    ingredients: RecipeIngredient[];
}

export interface RecipeIngredient {
    id: string;
    amount: number;
    productId?: string | null;
    productName?: string | null;
    productBaseUnit?: MeasurementUnit | string | null;
    nestedRecipeId?: string | null;
    nestedRecipeName?: string | null;
}

export interface RecipeFilters {
    search?: string | null;
}

export interface RecipeDto {
    name: string;
    description?: string | null;
    category?: string | null;
    imageUrl?: string | null;
    prepTime: number;
    cookTime: number;
    servings: number;
    visibility: RecipeVisibility;
    steps: RecipeStepDto[];
}

export interface RecipeStepDto {
    description: string;
    imageUrl?: string | null;
    ingredients: RecipeIngredientDto[];
}

export interface RecipeIngredientDto {
    productId?: string;
    nestedRecipeId?: string;
    amount: number;
}
