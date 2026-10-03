import type { Recipe } from '../../../shared/models/recipe.data';

export type ExploreFilters = {
    search?: string;
    category?: string;
    maxPrepTime?: number;
    sortBy?: 'newest' | 'popular';
};

export type ExploreRecipe = Recipe;
