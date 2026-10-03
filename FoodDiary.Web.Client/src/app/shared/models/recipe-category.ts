export const RECIPE_CATEGORIES = [
    'other',
    'breakfast',
    'soups',
    'salads',
    'main_courses',
    'side_dishes',
    'appetizers',
    'sandwiches',
    'pasta',
    'baking',
    'desserts',
    'drinks',
    'sauces',
    'snacks',
    'preserves',
] as const;
export type RecipeCategory = (typeof RECIPE_CATEGORIES)[number];
const categoryCodes = new Set<string>(RECIPE_CATEGORIES);
export function isRecipeCategory(value: string | null | undefined): value is RecipeCategory {
    return categoryCodes.has(value ?? '');
}
export function recipeCategoryKey(value: string | null | undefined): string {
    return `RECIPE_CATEGORIES.${isRecipeCategory(value) ? value : 'other'}`;
}
