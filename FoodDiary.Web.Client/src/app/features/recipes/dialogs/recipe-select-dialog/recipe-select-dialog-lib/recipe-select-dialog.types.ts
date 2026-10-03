import type { Recipe } from '../../../../../shared/models/recipe.data';

export type RecipeSelectItemViewModel = {
    recipe: Recipe;
    imageUrl: string | undefined;
};
