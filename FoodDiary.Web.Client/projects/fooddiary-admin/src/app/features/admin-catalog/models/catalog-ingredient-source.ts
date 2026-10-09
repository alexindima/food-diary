/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- decoded observations preserve import amounts without adding new validation */
import { type AdminId, adminId } from '../../../shared/models/semantics/admin-meaning';
import type { CatalogIngredient } from './catalog-transfer';

declare const catalogAmount: unique symbol;
export type CatalogProductAmount = number & { readonly [catalogAmount]: 'product-amount' };
export type CatalogRecipeServings = number & { readonly [catalogAmount]: 'recipe-servings' };

export type CatalogIngredientSource =
    | { kind: 'product'; productId: AdminId<'product'>; amount: CatalogProductAmount }
    | { kind: 'recipe'; recipeId: AdminId<'recipe'>; amount: CatalogRecipeServings }
    | { kind: 'text'; textName: string | null | undefined; amountText: string | null | undefined; amount: number }
    | { kind: 'legacy-dual'; productId: AdminId<'product'>; recipeId: AdminId<'recipe'>; amount: number };

export function decodeCatalogIngredientSource(value: CatalogIngredient): CatalogIngredientSource {
    if (value.productId !== null && value.nestedRecipeId !== null) {
        return {
            kind: 'legacy-dual',
            productId: adminId<'product'>(value.productId),
            recipeId: adminId<'recipe'>(value.nestedRecipeId),
            amount: value.amount,
        };
    }
    if (value.productId !== null) {
        return { kind: 'product', productId: adminId<'product'>(value.productId), amount: value.amount as CatalogProductAmount };
    }
    if (value.nestedRecipeId !== null) {
        return { kind: 'recipe', recipeId: adminId<'recipe'>(value.nestedRecipeId), amount: value.amount as CatalogRecipeServings };
    }
    return { kind: 'text', textName: value.textName, amountText: value.amountText, amount: value.amount };
}

export function catalogProductReference(source: CatalogIngredientSource): AdminId<'product'> | null {
    return source.kind === 'product' || source.kind === 'legacy-dual' ? source.productId : null;
}

export function catalogRecipeReference(source: CatalogIngredientSource): AdminId<'recipe'> | null {
    return source.kind === 'recipe' || source.kind === 'legacy-dual' ? source.recipeId : null;
}
