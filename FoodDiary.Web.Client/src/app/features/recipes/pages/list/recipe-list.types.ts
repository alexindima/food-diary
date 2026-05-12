import type { Recipe } from '../../models/recipe.data';

export interface RecipeCardViewModel {
    recipe: Recipe;
    imageUrl: string | undefined;
}
