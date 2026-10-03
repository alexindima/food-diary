import type { RecipeCategory } from '../../../shared/models/recipe-category';
export type PublicRecipeIngredient = {
    productId?: string | null;
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
    authorName?: string | null;
    language?: string;
    id: string;
    name: string;
    description: string | null;
    category: RecipeCategory | null;
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
    missingIngredientNames?: string[];
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
