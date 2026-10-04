export type RecipeImportRequest = { sourceUrl: string | null; text: string | null };

export type RecipeImportResult = {
    name: string;
    description: string | null;
    ingredients: Array<{ name: string; amount: string | null }>;
    steps: string[];
    servings: number | null;
    prepMinutes: number | null;
    cookMinutes: number | null;
    authorNutrition: string | null;
    sourceUrl: string | null;
};
