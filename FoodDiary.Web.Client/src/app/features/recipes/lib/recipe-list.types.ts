import type { Recipe } from '../../../shared/models/recipe.data';

export type RecipeCardViewModel = {
    recipe: Recipe;
    imageUrl: string | undefined;
};
