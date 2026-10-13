export type RecipeImportRequest = { sourceUrl: string | null; text: string | null };

export type RecipeImportNutrition = {
    basis: 'recipe' | 'serving' | 'per100g' | 'unknown';
    calories: number | null;
    proteins: number | null;
    fats: number | null;
    carbs: number | null;
    fiber: number | null;
    alcohol: number | null;
};

export type RecipeImportResult = {
    name: string;
    description: string | null;
    ingredients: Array<{ name: string; amount: string | null; stepIndex?: number | null }>;
    steps: string[];
    servings: number | null;
    prepMinutes: number | null;
    cookMinutes: number | null;
    authorNutrition: string | null;
    sourceUrl: string | null;
    nutrition?: RecipeImportNutrition | null;
};
