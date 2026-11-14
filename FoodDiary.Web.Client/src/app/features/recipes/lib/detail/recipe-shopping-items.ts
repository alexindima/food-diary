import type { Recipe, RecipeIngredient } from '../../../../shared/models/recipe.data';
import type { ShoppingListItemDto } from '../../../../shared/models/shopping-list.data';
import { resolveServingsUnitKey } from '../recipe-servings.utils';

const MAX_SHOPPING_AMOUNT = 1_000_000;

export function buildRecipeShoppingItems(
    recipe: Recipe,
    translate: (key: string) => string,
): Array<{ index: number; item: ShoppingListItemDto }> {
    return recipe.steps
        .flatMap(step => step.ingredients)
        .flatMap((ingredient, index) => {
            const item = toShoppingItem(ingredient, translate);
            return item === null ? [] : [{ index, item }];
        });
}

function toShoppingItem(ingredient: RecipeIngredient, translate: (key: string) => string): ShoppingListItemDto | null {
    const name = ingredientName(ingredient);
    if (name.length === 0) {
        return null;
    }

    const item = { name, isChecked: false };
    if (typeof ingredient.textName === 'string') {
        return { ...item, note: ingredient.amountText?.trim() ?? null };
    }

    const amount = shoppingAmount(ingredient.amount);
    const productId = ingredient.productId ?? null;
    if (productId !== null) {
        return { ...item, productId, amount, unit: ingredient.productBaseUnit ?? null };
    }

    const note =
        amount === null || typeof ingredient.nestedRecipeId !== 'string' ? null : `${amount} ${translate(resolveServingsUnitKey(amount))}`;
    return { ...item, note };
}

function ingredientName(ingredient: RecipeIngredient): string {
    return (ingredient.textName ?? ingredient.productName ?? ingredient.nestedRecipeName ?? '').trim();
}

function shoppingAmount(amount: number): number | null {
    return amount > 0 && amount <= MAX_SHOPPING_AMOUNT ? amount : null;
}
