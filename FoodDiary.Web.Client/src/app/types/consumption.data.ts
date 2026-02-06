import { Product, MeasurementUnit, ProductType, ProductVisibility } from './product.data';
import { Recipe, RecipeVisibility } from './recipe.data';

export interface Consumption {
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
    items: ConsumptionItem[];
    aiSessions?: ConsumptionAiSession[];
}

export interface ConsumptionItem {
    id: string;
    consumptionId: string;
    amount: number;
    sourceType: ConsumptionSourceType;
    product?: Product | null;
    recipe?: Recipe | null;
}

export interface ConsumptionAiSession {
    id: string;
    consumptionId: string;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: ConsumptionAiItem[];
}

export interface ConsumptionAiItem {
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
}

export interface ConsumptionResponseDto {
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
    items: ConsumptionItemResponseDto[];
    aiSessions?: ConsumptionAiSessionResponseDto[];
}

export interface ConsumptionItemResponseDto {
    id: string;
    consumptionId: string;
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
    recipeId?: string | null;
    recipeName?: string | null;
    recipeServings?: number | null;
    recipeTotalCalories?: number | null;
    recipeTotalProteins?: number | null;
    recipeTotalFats?: number | null;
    recipeTotalCarbs?: number | null;
    recipeTotalFiber?: number | null;
    recipeTotalAlcohol?: number | null;
}

export interface ConsumptionAiSessionResponseDto {
    id: string;
    consumptionId: string;
    imageAssetId?: string | null;
    imageUrl?: string | null;
    recognizedAtUtc: string;
    notes?: string | null;
    items: ConsumptionAiItemResponseDto[];
}

export interface ConsumptionAiItemResponseDto {
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
}

export enum ConsumptionSourceType {
    Product = 'Product',
    Recipe = 'Recipe',
}

export interface ConsumptionFilters {
    dateFrom?: string;
    dateTo?: string;
}

export interface ConsumptionManageDto {
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
}

export interface ConsumptionItemManageDto {
    productId?: string | null;
    recipeId?: string | null;
    amount: number;
}

export interface ConsumptionAiSessionManageDto {
    imageAssetId?: string | null;
    imageUrl?: string | null;
    recognizedAtUtc?: string | null;
    notes?: string | null;
    items: ConsumptionAiItemManageDto[];
}

export interface ConsumptionAiItemManageDto {
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
}

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
