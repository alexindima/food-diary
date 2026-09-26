import type { SeoData } from '../../../services/seo.service';
import type { PublicRecipe, PublicRecipeIngredient } from '../models/public-recipe.data';

export function recipeImages(recipe: Pick<PublicRecipe, 'imageUrl' | 'images'>): string[] {
    return [...new Set([recipe.imageUrl, ...recipe.images].filter((url): url is string => url !== null && url.length > 0))];
}

export function scaleIngredient(ingredient: PublicRecipeIngredient, servings: number, originalServings: number): number | null {
    return ingredient.amount === null ? null : (ingredient.amount * servings) / Math.max(1, originalServings);
}

export function publicRecipeSeo(recipe: PublicRecipe): SeoData {
    return {
        title: recipe.name,
        description: recipe.description ?? (recipe.steps.length > 0 ? recipe.steps[0].instruction : recipe.name),
        imageUrl: recipeImages(recipe)[0],
        recipeStructuredData: {
            '@type': 'Recipe',
            name: recipe.name,
            description: recipe.description ?? undefined,
            image: recipeImages(recipe),
            recipeCategory: recipe.category ?? undefined,
            recipeYield: String(recipe.servings),
            prepTime: recipe.prepTime === null ? undefined : `PT${recipe.prepTime}M`,
            cookTime: recipe.cookTime === null ? undefined : `PT${recipe.cookTime}M`,
            recipeIngredient: recipe.steps
                .flatMap(step => step.ingredients)
                .filter(item => item.isAvailable)
                .map(item =>
                    [item.name, item.amountText ?? [item.amount, item.unit].filter(value => value !== null).join(' ')].join(' — '),
                ),
            recipeInstructions: recipe.steps.map(step => ({
                '@type': 'HowToStep',
                name: step.title ?? undefined,
                text: step.instruction,
                image: step.images,
            })),
        },
    };
}
