export type RecipeLookup = {
    id: string;
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
