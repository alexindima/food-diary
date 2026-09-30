export type CatalogMeal = {
    mealType: string;
    recipeId: string;
    recipeName?: string | null;
    servings: number;
};

export type CatalogDay = {
    dayNumber: number;
    meals: CatalogMeal[];
};

export type CatalogPlanRequest = {
    name: string;
    description: string | null;
    dietType: string;
    durationDays: number;
    targetCaloriesPerDay: number | null;
    isPublished: boolean;
    days: CatalogDay[];
};

export type CatalogPlan = {
    id: string;
    name: string;
    description: string | null;
    dietType: string;
    durationDays: number;
    targetCaloriesPerDay: number | null;
    isCurated: boolean;
    days: CatalogDay[];
};

export type CatalogRecipe = {
    id: string;
    name: string;
    servings: number;
};

export const CATALOG_DIETS = ['Balanced', 'HighProtein', 'LowCarb', 'Keto', 'Mediterranean', 'Vegan', 'Vegetarian'];
export const CATALOG_MEALS = ['Breakfast', 'Lunch', 'Dinner', 'Snack', 'Other'];
