import type { Recipe } from '../models/recipe.data';

export interface RecipeSelectItemViewModel {
    recipe: Recipe;
    imageUrl: string | undefined;
}
