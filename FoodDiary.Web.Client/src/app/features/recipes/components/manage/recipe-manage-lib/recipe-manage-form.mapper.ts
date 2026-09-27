import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import { type Recipe, type RecipeDto, type RecipeIngredient, RecipeVisibility } from '../../../models/recipe.data';
import type { IngredientFormValues, NutritionScaleMode, RecipeFormValues, StepFormValues } from './recipe-manage.types';

export const RECIPE_TEXT_NAME_MAX_LENGTH = 256;
export const RECIPE_TEXT_AMOUNT_MAX_LENGTH = 128;
export const RECIPE_LONG_TEXT_MAX_LENGTH = 1_000;
export const RECIPE_STEP_TITLE_MAX_LENGTH = 120;
export const RECIPE_MIN_INGREDIENT_AMOUNT = 0.01;
export const RECIPE_DEFAULT_PRODUCT_QUALITY_SCORE = 50;

export type RecipeIngredientMappingLabels = {
    selectIngredient: string;
    unknownProduct: string;
};

type RecipeIngredientGroupInput = {
    textName?: string | null;
    amountText?: string | null;
    food?: Product | null;
    productId?: string | null;
    amount?: number | null;
    nestedRecipe?: Recipe | null;
    nestedRecipeId?: string | null;
    nestedRecipeName?: string | null;
};

export function createRecipeFormValue(language = 'en'): RecipeFormValues {
    return {
        language,
        name: '',
        description: '',
        comment: null,
        category: null,
        imageUrl: null,
        prepTime: 0,
        cookTime: null,
        servings: 1,
        visibility: RecipeVisibility.Private,
        calculateNutritionAutomatically: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        steps: [],
    };
}

export function createRecipeStepValue(step?: StepFormValues): StepFormValues {
    const ingredientValues = step?.ingredients ?? [];

    return {
        title: step?.title ?? null,
        images: step?.images,
        imageUrl: step?.imageUrl ?? null,
        description: step?.description ?? '',
        ingredients: ingredientValues.map(ingredient => createRecipeIngredientValue(ingredient)),
    };
}

export function createRecipeIngredientValue(input: RecipeIngredientGroupInput = {}): IngredientFormValues {
    const food = input.food ?? null;
    const nestedRecipe = input.nestedRecipe ?? null;
    const nestedRecipeName = input.nestedRecipeName ?? null;

    return {
        textName: input.textName,
        amountText: input.amountText,
        food,
        productId: input.productId ?? food?.id ?? null,
        amount: input.amount ?? null,
        foodName: resolveIngredientFoodName(food, nestedRecipe, nestedRecipeName, input.textName),
        nestedRecipe,
        nestedRecipeId: input.nestedRecipeId ?? null,
        nestedRecipeName,
    };
}

function resolveIngredientFoodName(
    food: Product | null,
    nestedRecipe: Recipe | null,
    nestedRecipeName: string | null,
    textName?: string | null,
): string | null {
    return textName ?? food?.name ?? nestedRecipe?.name ?? nestedRecipeName;
}

export function buildRecipeDto(
    formValue: RecipeFormValues,
    scaleMode: NutritionScaleMode,
    servings: number,
    toRecipeTotal: (value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number) => number,
): RecipeDto {
    return {
        language: formValue.language,
        name: formValue.name,
        description: formValue.description ?? null,
        comment: formValue.comment ?? null,
        category: formValue.category ?? null,
        imageUrl: formValue.imageUrl?.url ?? null,
        imageAssetId: formValue.imageUrl?.assetId ?? null,
        ...(formValue.images?.every(image => image.assetId !== null) === true
            ? { imageAssetIds: formValue.images.map(image => image.assetId).filter((id): id is string => id !== null) }
            : {}),
        prepTime: formValue.prepTime,
        cookTime: formValue.cookTime,
        servings: formValue.servings,
        visibility: formValue.visibility,
        calculateNutritionAutomatically: formValue.calculateNutritionAutomatically,
        ...buildManualRecipeTotals(formValue, scaleMode, servings, toRecipeTotal),
        steps: mapRecipeStepsToDto(formValue.steps),
    };
}

export function buildRecipeFormPatchValue(recipeData: Recipe): Partial<RecipeFormValues> {
    return {
        language: recipeData.language ?? 'en',
        ...((recipeData.images?.length ?? 0) > 0
            ? { images: (recipeData.images ?? []).map(image => ({ assetId: image.imageAssetId, url: image.imageUrl })) }
            : {}),
        name: recipeData.name,
        description: recipeData.description ?? '',
        comment: toNullable(recipeData.comment),
        category: toNullable(recipeData.category),
        imageUrl: {
            url: toNullable(recipeData.imageUrl),
            assetId: toNullable(recipeData.imageAssetId),
        },
        prepTime: withDefault(recipeData.prepTime, 0),
        cookTime: toNullable(recipeData.cookTime),
        servings: recipeData.servings,
        visibility: normalizeRecipeVisibility(recipeData.visibility),
        calculateNutritionAutomatically: recipeData.isNutritionAutoCalculated,
        ...buildRecipeManualNutritionPatchValue(recipeData),
    };
}

export function mapRecipeStepToFormValue(step: Recipe['steps'][number], labels: RecipeIngredientMappingLabels): StepFormValues {
    return {
        title: step.title ?? null,
        ...((step.images?.length ?? 0) > 0
            ? { images: (step.images ?? []).map(image => ({ assetId: image.imageAssetId, url: image.imageUrl })) }
            : {}),
        imageUrl: {
            url: step.imageUrl ?? null,
            assetId: step.imageAssetId ?? null,
        },
        description: step.instruction,
        ingredients: step.ingredients
            .map(ingredient => mapIngredientToFormValue(ingredient, labels))
            .filter((ingredient): ingredient is IngredientFormValues => ingredient !== null),
    };
}

export function hasNoRecipeNutritionTotals(recipeData: Recipe): boolean {
    return [recipeData.totalCalories, recipeData.totalProteins, recipeData.totalFats, recipeData.totalCarbs].every(
        value => value === null || value === undefined,
    );
}

export function normalizeRecipeVisibility(value?: RecipeVisibility | string | null): RecipeVisibility {
    if (value === null || value === undefined || value.length === 0) {
        return RecipeVisibility.Public;
    }

    return value.toString().toUpperCase() === RecipeVisibility.Private.toUpperCase() ? RecipeVisibility.Private : RecipeVisibility.Public;
}

function mapRecipeStepsToDto(steps: RecipeFormValues['steps']): RecipeDto['steps'] {
    return steps.map(step => mapRecipeStepToDto(step));
}

function mapRecipeStepToDto(step: RecipeFormValues['steps'][number]): RecipeDto['steps'][number] {
    return {
        title: step.title ?? null,
        imageUrl: step.imageUrl?.url ?? null,
        imageAssetId: step.imageUrl?.assetId ?? null,
        ...(step.images?.every(image => image.assetId !== null) === true
            ? { imageAssetIds: step.images.map(image => image.assetId).filter((id): id is string => id !== null) }
            : {}),
        description: step.description,
        ingredients: step.ingredients
            .filter(ingredient => typeof ingredient.textName === 'string' || hasProductId(ingredient) || hasNestedRecipeId(ingredient))
            .map(ingredient => ({
                productId: resolveProductId(ingredient),
                nestedRecipeId: resolveNestedRecipeId(ingredient),
                amount: typeof ingredient.textName === 'string' ? 0 : (ingredient.amount ?? 0),
                ...(typeof ingredient.textName === 'string'
                    ? { textName: ingredient.textName.trim(), amountText: ingredient.amountText?.trim() ?? null }
                    : {}),
            })),
    };
}

function hasProductId(ingredient: IngredientFormValues): boolean {
    return resolveProductId(ingredient) !== undefined;
}

function hasNestedRecipeId(ingredient: IngredientFormValues): boolean {
    return resolveNestedRecipeId(ingredient) !== undefined;
}

function resolveProductId(ingredient: IngredientFormValues): string | undefined {
    const productId = ingredient.productId ?? ingredient.food?.id ?? null;
    return productId !== null && productId.length > 0 ? productId : undefined;
}

function resolveNestedRecipeId(ingredient: IngredientFormValues): string | undefined {
    const nestedRecipeId = ingredient.nestedRecipeId ?? null;
    return nestedRecipeId !== null && nestedRecipeId.length > 0 ? nestedRecipeId : undefined;
}

function buildManualRecipeTotals(
    formValue: RecipeFormValues,
    scaleMode: NutritionScaleMode,
    servings: number,
    toRecipeTotal: (value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number) => number,
): Partial<RecipeDto> {
    const calculateAutomatically = formValue.calculateNutritionAutomatically;
    return {
        manualCalories: calculateAutomatically ? null : toRecipeTotal(formValue.manualCalories, scaleMode, servings),
        manualProteins: calculateAutomatically ? null : toRecipeTotal(formValue.manualProteins, scaleMode, servings),
        manualFats: calculateAutomatically ? null : toRecipeTotal(formValue.manualFats, scaleMode, servings),
        manualCarbs: calculateAutomatically ? null : toRecipeTotal(formValue.manualCarbs, scaleMode, servings),
        manualFiber: calculateAutomatically ? null : toRecipeTotal(formValue.manualFiber, scaleMode, servings),
        manualAlcohol: calculateAutomatically ? null : toRecipeTotal(formValue.manualAlcohol, scaleMode, servings),
    };
}

function buildRecipeManualNutritionPatchValue(recipeData: Recipe): Partial<RecipeFormValues> {
    return {
        manualCalories: resolveRecipeManualNutritionValue(recipeData.manualCalories, recipeData.totalCalories),
        manualProteins: resolveRecipeManualNutritionValue(recipeData.manualProteins, recipeData.totalProteins),
        manualFats: resolveRecipeManualNutritionValue(recipeData.manualFats, recipeData.totalFats),
        manualCarbs: resolveRecipeManualNutritionValue(recipeData.manualCarbs, recipeData.totalCarbs),
        manualFiber: resolveRecipeManualNutritionValue(recipeData.manualFiber, recipeData.totalFiber),
        manualAlcohol: resolveRecipeManualNutritionValue(recipeData.manualAlcohol, recipeData.totalAlcohol),
    };
}

function resolveRecipeManualNutritionValue(manual: number | null | undefined, total: number | null | undefined): number | null {
    return manual ?? total ?? null;
}

function mapIngredientToFormValue(ingredient: RecipeIngredient, labels: RecipeIngredientMappingLabels): IngredientFormValues | null {
    if (typeof ingredient.textName === 'string') {
        return createRecipeIngredientValue({ textName: ingredient.textName, amountText: ingredient.amountText });
    }
    if (ingredient.nestedRecipeId !== null && ingredient.nestedRecipeId !== undefined && ingredient.nestedRecipeId.length > 0) {
        return {
            food: null,
            productId: null,
            amount: ingredient.amount,
            foodName: ingredient.nestedRecipeName ?? labels.selectIngredient,
            nestedRecipe: buildNestedRecipe(ingredient),
            nestedRecipeId: ingredient.nestedRecipeId,
            nestedRecipeName: ingredient.nestedRecipeName ?? null,
        };
    }

    const product = buildIngredientProduct(ingredient, labels.unknownProduct);
    if (product === null) {
        return null;
    }

    return {
        food: product,
        productId: product.id,
        amount: ingredient.amount,
        foodName: product.name,
        nestedRecipe: null,
        nestedRecipeId: null,
        nestedRecipeName: null,
    };
}

function buildNestedRecipe(ingredient: RecipeIngredient): Recipe | null {
    const nestedRecipeId = ingredient.nestedRecipeId ?? null;
    if (nestedRecipeId === null || nestedRecipeId.length === 0) {
        return null;
    }

    return {
        id: nestedRecipeId,
        name: ingredient.nestedRecipeName ?? '',
        description: null,
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: null,
        cookTime: null,
        servings: ingredient.nestedRecipeServings ?? 1,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: new Date().toISOString(),
        isOwnedByCurrentUser: true,
        missingIngredientCount: ingredient.nestedRecipeMissingIngredientCount ?? 0,
        ...buildNestedRecipeNutrition(ingredient),
        isNutritionAutoCalculated: true,
        steps: [],
    };
}

function buildNestedRecipeNutrition(
    ingredient: RecipeIngredient,
): Pick<Recipe, 'totalAlcohol' | 'totalCalories' | 'totalCarbs' | 'totalFats' | 'totalFiber' | 'totalProteins'> {
    return {
        totalCalories: ingredient.nestedRecipeTotalCalories ?? null,
        totalProteins: ingredient.nestedRecipeTotalProteins ?? null,
        totalFats: ingredient.nestedRecipeTotalFats ?? null,
        totalCarbs: ingredient.nestedRecipeTotalCarbs ?? null,
        totalFiber: ingredient.nestedRecipeTotalFiber ?? null,
        totalAlcohol: ingredient.nestedRecipeTotalAlcohol ?? null,
    };
}

function buildIngredientProduct(ingredient: RecipeIngredient, unknownProductName: string): Product | null {
    if (ingredient.productId === null || ingredient.productId === undefined || ingredient.productId.length === 0) {
        return null;
    }

    const rawUnit = ingredient.productBaseUnit;
    const unit = isMeasurementUnit(rawUnit) ? rawUnit : MeasurementUnit.G;
    const baseAmount = ingredient.productBaseAmount ?? DEFAULT_NUTRITION_BASE_AMOUNT;

    return {
        id: ingredient.productId,
        name: ingredient.productName ?? unknownProductName,
        baseUnit: unit,
        productType: ProductType.Unknown,
        barcode: null,
        brand: null,
        category: null,
        description: null,
        imageUrl: null,
        baseAmount,
        defaultPortionAmount: baseAmount,
        ...buildIngredientProductNutrition(ingredient),
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date(),
        isOwnedByCurrentUser: true,
        qualityScore: RECIPE_DEFAULT_PRODUCT_QUALITY_SCORE,
        qualityGrade: 'yellow',
    };
}

function buildIngredientProductNutrition(
    ingredient: RecipeIngredient,
): Pick<Product, 'alcoholPerBase' | 'caloriesPerBase' | 'carbsPerBase' | 'fatsPerBase' | 'fiberPerBase' | 'proteinsPerBase'> {
    return {
        caloriesPerBase: ingredient.productCaloriesPerBase ?? 0,
        proteinsPerBase: ingredient.productProteinsPerBase ?? 0,
        fatsPerBase: ingredient.productFatsPerBase ?? 0,
        carbsPerBase: ingredient.productCarbsPerBase ?? 0,
        fiberPerBase: ingredient.productFiberPerBase ?? 0,
        alcoholPerBase: ingredient.productAlcoholPerBase ?? 0,
    };
}

function isMeasurementUnit(value: string | null | undefined): value is MeasurementUnit {
    return value === 'G' || value === 'ML' || value === 'PCS';
}

function toNullable<T>(value: T | null | undefined): T | null {
    return value ?? null;
}

function withDefault<T>(value: T | null | undefined, fallback: T): T {
    return value ?? fallback;
}
