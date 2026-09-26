import type { PublicRecipe } from '../models/public-recipe.data';
export function publicRecipeFixture(): PublicRecipe {
    return {
        id: 'recipe',
        name: 'Soup',
        description: 'A soup',
        category: 'Dinner',
        imageUrl: 'cover.jpg',
        images: ['cover.jpg', 'second.jpg'],
        prepTime: 10,
        cookTime: 20,
        servings: 2,
        totalCalories: 200,
        totalProteins: 10,
        totalFats: 5,
        totalCarbs: 20,
        totalFiber: 2,
        totalAlcohol: 0,
        missingIngredientCount: 1,
        steps: [{ stepNumber: 1, title: null, instruction: 'Boil', images: [], ingredients: [] }],
    };
}
