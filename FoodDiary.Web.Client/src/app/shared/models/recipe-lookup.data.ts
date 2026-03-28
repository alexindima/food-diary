export interface RecipeLookup {
    id: string;
    servings: number;
    steps: RecipeLookupStep[];
}

export interface RecipeLookupStep {
    ingredients: RecipeLookupIngredient[];
}

export interface RecipeLookupIngredient {
    amount: number;
    productBaseUnit: string | null;
}
