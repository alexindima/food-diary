import type { RecipeId } from './semantics/entity-id';
export type RecipeLookup = {
    id: RecipeId;
    servings: number;
    steps: RecipeLookupStep[];
};

export type RecipeLookupStep = {
    ingredients: RecipeLookupIngredient[];
};

export type RecipeLookupIngredient = {
    amount: number;
    productBaseUnit: string | null;
};
