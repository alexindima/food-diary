export type PublicRecipeIngredient = {
    name: string | null;
    amount: number | null;
    unit: string | null;
    amountText: string | null;
    recipeId: string | null;
    isAvailable: boolean;
};

export type PublicRecipeStep = {
    stepNumber: number;
    title: string | null;
    instruction: string;
    images: string[];
    ingredients: PublicRecipeIngredient[];
};

export type PublicRecipe = {
    language?: string;
    id: string;
    name: string;
    description: string | null;
    category: string | null;
    imageUrl: string | null;
    images: string[];
    prepTime: number | null;
    cookTime: number | null;
    servings: number;
    totalCalories: number | null;
    totalProteins: number | null;
    totalFats: number | null;
    totalCarbs: number | null;
    totalFiber: number | null;
    totalAlcohol: number | null;
    missingIngredientCount: number;
    steps: PublicRecipeStep[];
};

export type PublicRecipeFilters = {
    page: number;
    search: string;
    category: string;
    maxTotalTime?: number;
    sortBy?: string;
    language?: string;
};
