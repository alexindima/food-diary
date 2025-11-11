import { Product, MeasurementUnit, ProductVisibility } from './product.data';
import { Recipe, RecipeVisibility } from './recipe.data';

export interface Consumption {
    id: number;
    date: string;
    mealType?: string | null;
    comment?: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    items: ConsumptionItem[];
}

export interface ConsumptionItem {
    id: number;
    consumptionId: number;
    amount: number;
    sourceType: ConsumptionSourceType;
    product?: Product | null;
    recipe?: Recipe | null;
}

export interface ConsumptionResponseDto {
    id: number;
    date: string;
    mealType?: string | null;
    comment?: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    items: ConsumptionItemResponseDto[];
}

export interface ConsumptionItemResponseDto {
    id: number;
    consumptionId: number;
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
    recipeId?: string | null;
    recipeName?: string | null;
    recipeServings?: number | null;
    recipeTotalCalories?: number | null;
    recipeTotalProteins?: number | null;
    recipeTotalFats?: number | null;
    recipeTotalCarbs?: number | null;
    recipeTotalFiber?: number | null;
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
    items: ConsumptionItemManageDto[];
}

export interface ConsumptionItemManageDto {
    productId?: string | null;
    recipeId?: string | null;
    amount: number;
}

export const createEmptyProductSnapshot = (): Product => ({
    id: '',
    name: '',
    baseUnit: MeasurementUnit.G,
    baseAmount: 1,
    caloriesPerBase: 0,
    proteinsPerBase: 0,
    fatsPerBase: 0,
    carbsPerBase: 0,
    fiberPerBase: 0,
    visibility: ProductVisibility.Private,
    usageCount: 0,
    createdAt: new Date(),
    isOwnedByCurrentUser: true,
});

export const createEmptyRecipeSnapshot = (): Recipe => ({
    id: '',
    name: '',
    servings: 1,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '',
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
});
