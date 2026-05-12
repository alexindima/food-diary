import type { Recipe } from '../../models/recipe.data';

export type RecipeCardViewModel = {
    recipe: Recipe;
    imageUrl: string | undefined;
};
