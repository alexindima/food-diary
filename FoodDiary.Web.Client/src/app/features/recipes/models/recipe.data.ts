import type { NutrientData } from '../../../shared/models/charts.data';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import type { MeasurementUnit } from '../../products/models/product.data';
import type { RecipeCategory } from './recipe-category';

export enum RecipeVisibility {
    Private = 'Private',
    Public = 'Public',
}

export type Recipe = {
    language?: string;
    languageConfirmed?: boolean;
    id: string;
    name: string;
    description?: string | null;
    comment?: string | null;
    category?: RecipeCategory | null;
    imageUrl?: string | null;
    images?: Array<{ imageAssetId: string; imageUrl: string }>;
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
    missingIngredientCount?: number;
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
};

export type RecipeStep = {
    images?: Array<{ imageAssetId: string; imageUrl: string }>;
    id: string;
    stepNumber: number;
    title?: string | null;
    instruction: string;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    ingredients: RecipeIngredient[];
};

export type RecipeIngredient = {
    textName?: string | null;
    amountText?: string | null;
    nestedRecipeMissingIngredientCount?: number;
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
    nestedRecipeServings?: number | null;
    nestedRecipeTotalCalories?: number | null;
    nestedRecipeTotalProteins?: number | null;
    nestedRecipeTotalFats?: number | null;
    nestedRecipeTotalCarbs?: number | null;
    nestedRecipeTotalFiber?: number | null;
    nestedRecipeTotalAlcohol?: number | null;
};

export type RecipeFilters = {
    search?: string | null;
    category?: string | null;
    maxTotalTime?: number | null;
    caloriesFrom?: number | null;
    caloriesTo?: number | null;
    hasImage?: boolean | null;
};

export type RecipeOverview = {
    recentItems: Recipe[];
    allRecipes: PageOf<Recipe>;
    favoriteItems: FavoriteRecipe[];
    favoriteTotalCount: number;
};

export type FavoriteRecipe = {
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
    ingredientNames?: string[];
    totalProteins?: number;
    totalFats?: number;
    totalCarbs?: number;
    totalFiber?: number;
};

export type RecipeDto = {
    language?: string;
    languageConfirmed?: boolean;
    name: string;
    description?: string | null;
    comment?: string | null;
    category?: RecipeCategory | null;
    imageUrl?: string | null;
    imageAssetIds?: string[];
    imageAssetId?: string | null;
    prepTime?: number | null;
    cookTime?: number | null;
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
};

export type RecipeStepDto = {
    imageAssetIds?: string[];
    title?: string | null;
    description: string;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    ingredients: RecipeIngredientDto[];
};

export type RecipeIngredientDto = {
    textName?: string | null;
    amountText?: string | null;
    productId?: string;
    nestedRecipeId?: string;
    amount: number;
};
