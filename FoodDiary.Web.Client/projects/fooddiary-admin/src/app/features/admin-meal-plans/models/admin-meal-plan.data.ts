import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
export type CatalogMeal = {
    mealType: string;
    recipeId: AdminId<'recipe'>;
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
    id: AdminId<'meal-plan'>;
    name: string;
    description: string | null;
    dietType: string;
    durationDays: number;
    targetCaloriesPerDay: number | null;
    isCurated: boolean;
    days: CatalogDay[];
};

export type CatalogPlanSummary = {
    id: AdminId<'meal-plan'>;
    name: string;
    description: string | null;
    dietType: string;
    durationDays: number;
    targetCaloriesPerDay: number | null;
    isCurated: boolean;
};

export type CatalogRecipe = {
    id: AdminId<'recipe'>;
    name: string;
    servings: number;
};

export const CATALOG_DIETS = ['Balanced', 'HighProtein', 'LowCarb', 'Keto', 'Mediterranean', 'Vegan', 'Vegetarian'];
export const CATALOG_MEALS = ['Breakfast', 'Lunch', 'Dinner', 'Snack', 'Other'];
