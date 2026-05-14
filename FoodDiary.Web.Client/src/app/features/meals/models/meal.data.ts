import type { PageOf } from '../../../shared/models/page-of.data';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../recipes/models/recipe.data';

export type Consumption = {
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
    items: ConsumptionItem[];
    aiSessions?: ConsumptionAiSession[];
};

export type ConsumptionItem = {
    id: string;
    consumptionId: string;
    amount: number;
    sourceType: ConsumptionSourceType;
    product?: Product | null;
    recipe?: Recipe | null;
};

export type ConsumptionAiSession = {
    id: string;
    consumptionId: string;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: ConsumptionAiItem[];
};

export type ConsumptionAiItem = {
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
};

export type ConsumptionResponseDto = {
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
    items: ConsumptionItemResponseDto[];
    aiSessions?: ConsumptionAiSessionResponseDto[];
};

export type ConsumptionOverview = {
    allConsumptions: PageOf<Meal>;
    favoriteItems: FavoriteMeal[];
    favoriteTotalCount: number;
};

export type ConsumptionItemResponseDto = {
    id: string;
    consumptionId: string;
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
};

export type ConsumptionAiSessionResponseDto = {
    id: string;
    consumptionId: string;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: ConsumptionAiItemResponseDto[];
};

export type ConsumptionAiItemResponseDto = {
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
};

export enum ConsumptionSourceType {
    Product = 'Product',
    Recipe = 'Recipe',
}

export type ConsumptionFilters = {
    dateFrom?: string;
    dateTo?: string;
};

export type ConsumptionManageDto = {
    date: Date;
    mealType?: string | null;
    comment?: string;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    items: ConsumptionItemManageDto[];
    isNutritionAutoCalculated: boolean;
    manualCalories?: number | null;
    manualProteins?: number | null;
    manualFats?: number | null;
    manualCarbs?: number | null;
    manualFiber?: number | null;
    manualAlcohol?: number | null;
    preMealSatietyLevel?: number | null;
    postMealSatietyLevel?: number | null;
    aiSessions?: ConsumptionAiSessionManageDto[];
};

export type ConsumptionItemManageDto = {
    productId?: string | null;
    recipeId?: string | null;
    amount: number;
};

export type ConsumptionAiSessionManageDto = {
    imageAssetId?: string | null;
    imageUrl?: string | null;
    source?: string | null;
    recognizedAtUtc?: string | null;
    notes?: string | null;
    items: ConsumptionAiItemManageDto[];
};

export type ConsumptionAiItemManageDto = {
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

export type Meal = Consumption;
export type MealItem = ConsumptionItem;
export type MealAiSession = ConsumptionAiSession;
export type MealAiItem = ConsumptionAiItem;
export type MealFilters = ConsumptionFilters;
export type MealManageDto = ConsumptionManageDto;
export type MealAiSessionManageDto = ConsumptionAiSessionManageDto;
export type MealOverview = ConsumptionOverview;

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
