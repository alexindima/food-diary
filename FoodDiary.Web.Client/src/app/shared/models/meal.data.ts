import type { PageOf } from './page-of.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from './product.data';
import type { QualityGrade } from './quality-grade.data';
import { type Recipe, RecipeVisibility } from './recipe.data';
import type { UtcInstant } from './semantics/date-value';
import { utcInstant } from './semantics/date-value';
import type { FavoriteMealId, ImageAssetId, MealId, MealItemId } from './semantics/entity-id';
import { entityId } from './semantics/entity-id';
import { type ProductQuantity, productQuantityFromStored, type RecipeServings, recipeServingsFromStored } from './semantics/meal-quantity';

export type Meal = {
    id: MealId;
    date: UtcInstant;
    mealType?: string | null;
    comment?: string | null;
    imageUrl?: string | null;
    imageAssetId?: ImageAssetId | null;
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
    favoriteMealId?: FavoriteMealId | null;
    items: MealItem[];
    aiSessions?: MealAiSession[];
};

type MealItemIdentity = {
    id: MealItemId;
    mealId: MealId;
    sourceAiItemId?: string | null;
    origin?: string | null;
};

export type ProductMealItem = MealItemIdentity & {
    sourceType: MealSourceType.Product;
    amount: ProductQuantity;
    product: Product | null;
    recipe: null;
};

export type RecipeMealItem = MealItemIdentity & {
    sourceType: MealSourceType.Recipe;
    amount: RecipeServings;
    product: null;
    recipe: Recipe | null;
};

export type LegacyDualSourceMealItem = MealItemIdentity & {
    sourceType: MealSourceType.Product;
    amount: ProductQuantity;
    product: Product;
    recipe: Recipe;
    legacyDualSource: true;
};

export type MealItem = ProductMealItem | RecipeMealItem | LegacyDualSourceMealItem;

/** Existing projections prefer the product when both sources occur; retain both snapshots. */
export function mealItemFromStored(
    identity: MealItemIdentity,
    amount: number,
    sources: { product: Product | null; recipe: Recipe | null; sourceType?: MealSourceType },
): MealItem {
    const { product, recipe } = sources;
    const sourceType = sources.sourceType ?? (product === null ? MealSourceType.Recipe : MealSourceType.Product);
    if (product !== null && recipe !== null) {
        return {
            ...identity,
            sourceType: MealSourceType.Product,
            amount: productQuantityFromStored(amount),
            product,
            recipe,
            legacyDualSource: true,
        };
    }
    return sourceType === MealSourceType.Product
        ? { ...identity, sourceType: MealSourceType.Product, amount: productQuantityFromStored(amount), product, recipe: null }
        : { ...identity, sourceType: MealSourceType.Recipe, amount: recipeServingsFromStored(amount), product: null, recipe };
}

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

export type MealDaySummary = { date: string; totalCalories: number; mealCount: number };

export type MealOverview = {
    daySummaries?: MealDaySummary[];
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
    id: entityId<'product'>(''),
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
    id: entityId<'recipe'>(''),
    name: '',
    comment: null,
    servings: 1,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: utcInstant(''),
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
});

export type FavoriteMeal = {
    itemImageUrls?: string[];
    imageUrl?: string | null;
    totalFiber?: number;
    itemNames?: string[];
    id: FavoriteMealId;
    mealId: MealId;
    name: string | null;
    createdAtUtc: UtcInstant;
    mealDate: UtcInstant;
    mealType: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    itemCount: number;
};
