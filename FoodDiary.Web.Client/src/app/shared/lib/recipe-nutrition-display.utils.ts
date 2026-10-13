/** Display stored whole-recipe nutrition without changing the recipe used by meals. */
export function recipeNutrientPerServing(value: number | null | undefined, servings: number | null | undefined): number | null {
    if (value === null || value === undefined) {
        return null;
    }
    const count = typeof servings === 'number' && Number.isFinite(servings) && servings >= 1 ? servings : 1;
    return value / count;
}
