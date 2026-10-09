import type { MeasurementUnit } from './product.data';
import { type EntityId, entityId, optionalEntityId, type ProductId, type RecipeId } from './semantics/entity-id';
import {
    type NestedRecipeIngredientServings,
    nestedRecipeIngredientServingsFromStored,
    type ObservedIngredientAmount,
    observedIngredientAmountFromStored,
    type ProductIngredientAmount,
    productIngredientAmountFromStored,
} from './semantics/recipe-quantity';

/** Scalar transport/stored observations, including missing and contradictory historical sources. */
export type RecipeIngredientFields = {
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

type IngredientSnapshot = Omit<RecipeIngredientFields, 'id' | 'amount' | 'productId' | 'nestedRecipeId' | 'textName'> & {
    id: EntityId<'recipe-ingredient'>;
};

export type RecipeIngredient = IngredientSnapshot &
    (
        | { kind: 'product'; productId: ProductId; nestedRecipeId?: null; textName?: null; amount: ProductIngredientAmount }
        | { kind: 'recipe'; productId?: null; nestedRecipeId: RecipeId; textName?: null; amount: NestedRecipeIngredientServings }
        | { kind: 'text'; productId?: null; nestedRecipeId?: null; textName: string; amount: ObservedIngredientAmount }
        | {
              kind: 'legacy';
              productId?: ProductId | null;
              nestedRecipeId?: RecipeId | null;
              textName?: string | null;
              amount: ObservedIngredientAmount;
          }
    );

/** Decode once at the read boundary. Legacy observations retain every field and consumer precedence. */
export function recipeIngredientFromStored(fields: RecipeIngredientFields): RecipeIngredient {
    const id = entityId<'recipe-ingredient'>(fields.id);
    const { productId, nestedRecipeId, textName } = fields;
    if (isAbsent(nestedRecipeId) && isAbsent(textName) && isSourceId(productId)) {
        return {
            ...fields,
            id,
            kind: 'product',
            productId: entityId<'product'>(productId),
            nestedRecipeId,
            textName,
            amount: productIngredientAmountFromStored(fields.amount),
        };
    }
    if (isAbsent(productId) && isAbsent(textName) && isSourceId(nestedRecipeId)) {
        return {
            ...fields,
            id,
            kind: 'recipe',
            nestedRecipeId: entityId<'recipe'>(nestedRecipeId),
            productId,
            textName,
            amount: nestedRecipeIngredientServingsFromStored(fields.amount),
        };
    }
    if (typeof textName === 'string' && isAbsent(productId) && isAbsent(nestedRecipeId)) {
        return {
            ...fields,
            id,
            kind: 'text',
            productId,
            nestedRecipeId,
            textName,
            amount: observedIngredientAmountFromStored(fields.amount),
        };
    }
    return {
        ...fields,
        id,
        kind: 'legacy',
        productId: optionalEntityId<'product'>(productId),
        nestedRecipeId: optionalEntityId<'recipe'>(nestedRecipeId),
        amount: observedIngredientAmountFromStored(fields.amount),
    };
}

/** The editor historically selects text, then a nested recipe, then a product. */
export function ingredientEditorSource(ingredient: RecipeIngredient): 'product' | 'recipe' | 'text' | 'missing' {
    if (ingredient.kind !== 'legacy') {
        return ingredient.kind;
    }
    if (typeof ingredient.textName === 'string') {
        return 'text';
    }
    if (isSourceId(ingredient.nestedRecipeId)) {
        return 'recipe';
    }
    return isSourceId(ingredient.productId) ? 'product' : 'missing';
}

function isAbsent(value: string | null | undefined): value is null | undefined {
    return value === null || value === undefined;
}

function isSourceId(value: string | null | undefined): value is string {
    return typeof value === 'string' && value.length > 0;
}
