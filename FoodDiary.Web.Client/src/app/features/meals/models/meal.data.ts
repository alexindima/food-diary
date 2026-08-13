import type { PageOf } from '../../../shared/models/page-of.data';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../recipes/models/recipe.data';

export type Meal = {
    id: string;
    date: string;
    mealType?: string | null;
    comment?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    totalAlcohol: number;
    isNutritionAutoCalculated: boolean;
    manualCalories?: number | null;
    manualProteins?: number | null;
    manualFats?: number | null;
    manualCarbs?: number | null;
    manualFiber?: number | null;
    manualAlcohol?: number | null;
    preMealSatietyLevel?: number | null;
    postMealSatietyLevel?: number | null;
    qualityScore?: number | null;
    qualityGrade?: QualityGrade | null;
    isFavorite?: boolean;
    favoriteMealId?: string | null;
    items: MealItem[];
    aiSessions?: MealAiSession[];
};

export type MealItem = {
    id: string;
    mealId: string;
    amount: number;
    sourceType: MealSourceType;
    sourceAiItemId?: string | null;
    origin?: string | null;
    product?: Product | null;
    recipe?: Recipe | null;
};

export type MealAiSession = {
    id: string;
    mealId: string;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    status?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: MealAiItem[];
};

export type MealAiItem = {
    id: string;
    sessionId: string;
    nameEn: string;
    nameLocal?: string | null;
    amount: number;
    unit: string;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
    confidence?: number | null;
    resolution?: string | null;
};

export type MealResponseDto = {
    id: string;
    date: string;
    mealType?: string | null;
    comment?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    totalAlcohol: number;
    isNutritionAutoCalculated: boolean;
    manualCalories?: number | null;
    manualProteins?: number | null;
    manualFats?: number | null;
    manualCarbs?: number | null;
    manualFiber?: number | null;
    manualAlcohol?: number | null;
    preMealSatietyLevel?: number | null;
    postMealSatietyLevel?: number | null;
    qualityScore?: number | null;
    qualityGrade?: QualityGrade | null;
    isFavorite?: boolean;
    favoriteMealId?: string | null;
    items: MealItemResponseDto[];
    aiSessions?: MealAiSessionResponseDto[];
};

export type MealOverview = {
    allMeals: PageOf<Meal>;
    favoriteItems: FavoriteMeal[];
    favoriteTotalCount: number;
};

export type MealItemResponseDto = {
    id: string;
    mealId: string;
    amount: number;
    productId?: string | null;
    productName?: string | null;
    productImageUrl?: string | null;
    productBaseUnit?: MeasurementUnit | string | null;
    productBaseAmount?: number | null;
    productCaloriesPerBase?: number | null;
    productProteinsPerBase?: number | null;
    productFatsPerBase?: number | null;
    productCarbsPerBase?: number | null;
    productFiberPerBase?: number | null;
    productAlcoholPerBase?: number | null;
    recipeId?: string | null;
    recipeName?: string | null;
    recipeImageUrl?: string | null;
    recipeServings?: number | null;
    recipeTotalCalories?: number | null;
    recipeTotalProteins?: number | null;
    recipeTotalFats?: number | null;
    recipeTotalCarbs?: number | null;
    recipeTotalFiber?: number | null;
    recipeTotalAlcohol?: number | null;
    productQualityScore?: number | null;
    productQualityGrade?: string | null;
    sourceAiItemId?: string | null;
    origin?: string | null;
};

export type MealAiSessionResponseDto = {
    id: string;
    mealId: string;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    status?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: MealAiItemResponseDto[];
};

export type MealAiItemResponseDto = {
    id: string;
    sessionId: string;
    nameEn: string;
    nameLocal?: string | null;
    amount: number;
    unit: string;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
    confidence?: number | null;
    resolution?: string | null;
};

export enum MealSourceType {
    Product = 'Product',
    Recipe = 'Recipe',
}

export type MealFilters = {
    dateFrom?: string;
    dateTo?: string;
    mealTypes?: string;
    caloriesFrom?: number;
    caloriesTo?: number;
    hasImage?: boolean;
    hasAiSession?: boolean;
};

export type MealManageDto = {
    date: Date;
    mealType?: string | null;
    comment?: string;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    items: MealItemManageDto[];
    isNutritionAutoCalculated: boolean;
    manualCalories?: number | null;
    manualProteins?: number | null;
    manualFats?: number | null;
    manualCarbs?: number | null;
    manualFiber?: number | null;
    manualAlcohol?: number | null;
    preMealSatietyLevel?: number | null;
    postMealSatietyLevel?: number | null;
    aiSessions?: MealAiSessionManageDto[];
};

export type MealItemManageDto = {
    productId?: string | null;
    recipeId?: string | null;
    amount: number;
    sourceAiItemId?: string | null;
    origin?: string | null;
};

export type MealAiSessionManageDto = {
    imageAssetId?: string | null;
    imageUrl?: string | null;
    source?: string | null;
    status?: string | null;
    recognizedAtUtc?: string | null;
    notes?: string | null;
    items: MealAiItemManageDto[];
};

export type MealAiItemManageDto = {
    nameEn: string;
    nameLocal?: string | null;
    amount: number;
    unit: string;
    calories: number;
    proteins: number;
    fats: number;
    carbs: number;
    fiber: number;
    alcohol: number;
    confidence?: number | null;
    resolution?: string | null;
};

export const createEmptyProductSnapshot = (): Product => ({
    id: '',
    name: '',
    productType: ProductType.Unknown,
    baseUnit: MeasurementUnit.G,
    baseAmount: 1,
    defaultPortionAmount: 1,
    caloriesPerBase: 0,
    proteinsPerBase: 0,
    fatsPerBase: 0,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    visibility: ProductVisibility.Private,
    usageCount: 0,
    createdAt: new Date(),
    isOwnedByCurrentUser: true,
    qualityScore: 50,
    qualityGrade: 'yellow',
});

export const createEmptyRecipeSnapshot = (): Recipe => ({
    id: '',
    name: '',
    comment: null,
    servings: 1,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '',
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
});

export type FavoriteMeal = {
    id: string;
    mealId: string;
    name: string | null;
    createdAtUtc: string;
    mealDate: string;
    mealType: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    itemCount: number;
};
