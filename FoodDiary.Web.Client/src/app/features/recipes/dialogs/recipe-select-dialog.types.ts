import type { Recipe } from '../models/recipe.data';

export type RecipeSelectItemViewModel = {
    recipe: Recipe;
    imageUrl: string | undefined;
};
