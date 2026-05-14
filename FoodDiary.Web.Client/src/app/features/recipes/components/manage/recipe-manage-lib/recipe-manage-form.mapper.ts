import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { nonEmptyArrayValidator } from '../../../../../validators/non-empty-array.validator';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import { type Recipe, type RecipeDto, type RecipeIngredient, RecipeVisibility } from '../../../models/recipe.data';
import type {
    IngredientFormData,
    IngredientFormValues,
    NutritionScaleMode,
    RecipeFormData,
    RecipeFormValues,
    StepFormData,
    StepFormValues,
} from './recipe-manage.types';

export const RECIPE_LONG_TEXT_MAX_LENGTH = 1_000;
export const RECIPE_STEP_TITLE_MAX_LENGTH = 120;
export const RECIPE_MIN_INGREDIENT_AMOUNT = 0.01;
export const RECIPE_DEFAULT_PRODUCT_QUALITY_SCORE = 50;

export type RecipeIngredientMappingLabels = {
    selectIngredient: string;
    unknownProduct: string;
};

export function createRecipeForm(): FormGroup<RecipeFormData> {
    return new FormGroup<RecipeFormData>({
        name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
        description: new FormControl('', [Validators.maxLength(RECIPE_LONG_TEXT_MAX_LENGTH)]),
        comment: new FormControl<string | null>(null, [Validators.maxLength(RECIPE_LONG_TEXT_MAX_LENGTH)]),
        category: new FormControl<string | null>(null),
        imageUrl: new FormControl<ImageSelection | null>(null),
        prepTime: new FormControl<number | null>(0, [Validators.min(0)]),
        cookTime: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
        servings: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
        visibility: new FormControl<RecipeVisibility>(RecipeVisibility.Public, { nonNullable: true }),
        calculateNutritionAutomatically: new FormControl<boolean>(true, { nonNullable: true }),
        manualCalories: new FormControl<number | null>(null, [Validators.min(0)]),
        manualProteins: new FormControl<number | null>(null, [Validators.min(0)]),
        manualFats: new FormControl<number | null>(null, [Validators.min(0)]),
        manualCarbs: new FormControl<number | null>(null, [Validators.min(0)]),
        manualFiber: new FormControl<number | null>(null, [Validators.min(0)]),
        manualAlcohol: new FormControl<number | null>(null, [Validators.min(0)]),
        steps: new FormArray<FormGroup<StepFormData>>([], nonEmptyArrayValidator()),
    });
}

export function createRecipeStepGroup(step?: StepFormValues): FormGroup<StepFormData> {
    const ingredientValues =
        step !== undefined && step.ingredients.length > 0
            ? step.ingredients
            : [{ food: null, amount: null, foodName: null, nestedRecipeId: null, nestedRecipeName: null }];

    return new FormGroup<StepFormData>({
        title: new FormControl(step?.title ?? null, [Validators.maxLength(RECIPE_STEP_TITLE_MAX_LENGTH)]),
        imageUrl: new FormControl<ImageSelection | null>(step?.imageUrl ?? null),
        description: new FormControl(step?.description ?? '', {
            nonNullable: true,
            validators: [Validators.required],
        }),
        ingredients: new FormArray<FormGroup<IngredientFormData>>(
            ingredientValues.map(ingredient =>
                createRecipeIngredientGroup(ingredient.food, ingredient.amount, ingredient.nestedRecipeId, ingredient.nestedRecipeName),
            ),
            nonEmptyArrayValidator(),
        ),
    });
}

export function createRecipeIngredientGroup(
    food: Product | null = null,
    amount: number | null = null,
    nestedRecipeId: string | null = null,
    nestedRecipeName: string | null = null,
): FormGroup<IngredientFormData> {
    return new FormGroup<IngredientFormData>({
        food: new FormControl(food),
        amount: new FormControl(amount, [Validators.required, Validators.min(RECIPE_MIN_INGREDIENT_AMOUNT)]),
        foodName: new FormControl<string | null>(food?.name ?? nestedRecipeName ?? null, [Validators.required]),
        nestedRecipeId: new FormControl<string | null>(nestedRecipeId),
        nestedRecipeName: new FormControl<string | null>(nestedRecipeName),
    });
}

export function buildRecipeDto(
    formValue: RecipeFormValues,
    scaleMode: NutritionScaleMode,
    servings: number,
    toRecipeTotal: (value: number | null | undefined, scaleMode: NutritionScaleMode, servings: number) => number,
): RecipeDto {
    return {
        name: formValue.name,
        description: formValue.description ?? null,
        comment: formValue.comment ?? null,
        category: formValue.category ?? null,
        imageUrl: formValue.imageUrl?.url ?? null,
        imageAssetId: formValue.imageUrl?.assetId ?? null,
        prepTime: formValue.prepTime ?? 0,
        cookTime: formValue.cookTime ?? 0,
        servings: formValue.servings,
        visibility: formValue.visibility,
        calculateNutritionAutomatically: formValue.calculateNutritionAutomatically,
        ...buildManualRecipeTotals(formValue, scaleMode, servings, toRecipeTotal),
        steps: mapRecipeStepsToDto(formValue.steps),
    };
}

export function buildRecipeFormPatchValue(recipeData: Recipe): Partial<RecipeFormValues> {
    return {
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
    return steps.map(step => mapRecipeStepToDto(step)).filter(step => step.ingredients.length > 0);
}

function mapRecipeStepToDto(step: RecipeFormValues['steps'][number]): RecipeDto['steps'][number] {
    return {
        title: step.title ?? null,
        imageUrl: step.imageUrl?.url ?? null,
        imageAssetId: step.imageUrl?.assetId ?? null,
        description: step.description,
        ingredients: step.ingredients
            .filter(ingredient => ingredient.food !== null || ingredient.nestedRecipeId !== null)
            .map(ingredient => ({
                productId: ingredient.food?.id,
                nestedRecipeId: ingredient.nestedRecipeId ?? undefined,
                amount: ingredient.amount ?? 0,
            })),
    };
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
    if (ingredient.nestedRecipeId !== null && ingredient.nestedRecipeId !== undefined && ingredient.nestedRecipeId.length > 0) {
        return {
            food: null,
            amount: ingredient.amount,
            foodName: ingredient.nestedRecipeName ?? labels.selectIngredient,
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
        amount: ingredient.amount,
        foodName: product.name,
        nestedRecipeId: null,
        nestedRecipeName: null,
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
