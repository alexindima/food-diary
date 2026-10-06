import { resolveRussianPluralCategory } from '../../../shared/i18n/russian-plural.utils';

export function resolveServingsUnitKey(count: number): string {
    const category = Number.isInteger(count) ? resolveRussianPluralCategory(count) : 'few';
    return `RECIPE_DETAIL.SUMMARY.SERVINGS_${category.toUpperCase()}`;
}

export function resolveIngredientUnitKey(ingredient: {
    nestedRecipeId?: string | null;
    productBaseUnit?: string | null;
    amount: number;
}): string | null {
    if (typeof ingredient.nestedRecipeId === 'string' && ingredient.nestedRecipeId.length > 0) {
        return resolveServingsUnitKey(ingredient.amount);
    }
    return typeof ingredient.productBaseUnit === 'string' && ingredient.productBaseUnit.length > 0
        ? `GENERAL.UNITS.${ingredient.productBaseUnit.toUpperCase()}`
        : null;
}
