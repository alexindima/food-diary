import { type NutrientData } from '../../../shared/models/charts.data';
import { type PageOf } from '../../../shared/models/page-of.data';
import { type MeasurementUnit } from '../../products/models/product.data';
import { type QualityGrade } from '../../products/models/product.data';

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
    qualityScore?: number | null;
    qualityGrade?: QualityGrade | null;
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
    isFavorite?: boolean;
    favoriteRecipeId?: string | null;
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

export interface RecipeOverview {
    recentItems: Recipe[];
    allRecipes: PageOf<Recipe>;
    favoriteItems: FavoriteRecipe[];
    favoriteTotalCount: number;
}

export interface FavoriteRecipe {
    id: string;
    recipeId: string;
    name?: string | null;
    createdAtUtc: string;
    recipeName: string;
    imageUrl?: string | null;
    totalCalories?: number | null;
    servings: number;
    totalTimeMinutes?: number | null;
    ingredientCount: number;
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
