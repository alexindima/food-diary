import { inject, Service } from '@angular/core';
import { catchError, map, type Observable, of } from 'rxjs';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import type { Recipe, RecipeIngredient } from '../../../../shared/models/recipe.data';
import type { RecipeLookup, RecipeLookupIngredient } from '../../../../shared/models/recipe-lookup.data';

@Service()
export class RecipeServingWeightService {
    private readonly recipeLookupService = inject(RecipeLookupService);
    private readonly cache = new Map<string, { recipeKey: string; weight: number | null }>();

    public loadServingWeight(recipe: Recipe | null): Observable<number | null> {
        if (recipe?.id === undefined || recipe.id.length === 0) {
            return of(null);
        }

        const cached = this.cache.get(recipe.id);
        if (cached?.recipeKey === this.recipeWeightKey(recipe)) {
            return of(cached.weight);
        }

        const immediateWeight = this.calculateRecipeWeight(recipe);
        if (immediateWeight !== null && immediateWeight > 0 && recipe.servings > 0) {
            const servingWeight = immediateWeight / recipe.servings;
            this.storeServingWeight(recipe, servingWeight);
            return of(servingWeight);
        }

        return this.recipeLookupService.getById(recipe.id).pipe(
            map(fullRecipe => {
                const computedWeight = this.calculateRecipeWeight(fullRecipe);
                if (computedWeight !== null && computedWeight > 0 && fullRecipe.servings > 0) {
                    const servingWeight = computedWeight / fullRecipe.servings;
                    this.storeServingWeight(recipe, servingWeight);
                    return servingWeight;
                }
                this.storeServingWeight(recipe, null);
                return null;
            }),
            catchError(() => {
                this.storeServingWeight(recipe, null);
                return of(null);
            }),
        );
    }

    public convertServingsToGrams(recipe: Recipe | null, servingsAmount: number): number {
        const servingWeight = this.cachedServingWeight(recipe);
        if (servingWeight !== null && servingWeight > 0) {
            return servingsAmount * servingWeight;
        }
        return servingsAmount;
    }

    public hasServingWeight(recipe: Recipe | null): boolean {
        const weight = this.cachedServingWeight(recipe);
        return weight !== null && Number.isFinite(weight) && weight > 0;
    }

    public convertGramsToServings(recipe: Recipe | null, grams: number): number {
        const servingWeight = this.cachedServingWeight(recipe);
        if (servingWeight !== null && servingWeight > 0) {
            return grams / servingWeight;
        }
        return grams;
    }

    private cachedServingWeight(recipe: Recipe | null): number | null {
        if (recipe === null) {
            return null;
        }
        const cached = this.cache.get(recipe.id);
        return cached?.recipeKey === this.recipeWeightKey(recipe) ? cached.weight : null;
    }

    private storeServingWeight(recipe: Recipe, weight: number | null): void {
        this.cache.set(recipe.id, { recipeKey: this.recipeWeightKey(recipe), weight });
    }

    private recipeWeightKey(recipe: Recipe): string {
        return JSON.stringify([
            recipe.servings,
            recipe.steps.flatMap(step =>
                step.ingredients.map(ingredient => [ingredient.amount, ingredient.productBaseUnit?.toString().toUpperCase()]),
            ),
        ]);
    }

    private calculateRecipeWeight(recipe: Recipe | RecipeLookup): number | null {
        if (recipe.steps.length === 0) {
            return null;
        }

        let total = 0;
        for (const step of recipe.steps) {
            for (const ingredient of step.ingredients) {
                const weight = this.calculateIngredientWeight(ingredient);
                if (weight === null) {
                    return null;
                }
                total += weight;
            }
        }

        return total > 0 ? total : null;
    }

    private calculateIngredientWeight(ingredient: RecipeIngredient | RecipeLookupIngredient): number | null {
        const amount = ingredient.amount;
        if (!Number.isFinite(amount) || amount <= 0) {
            return null;
        }

        const unitRaw = ingredient.productBaseUnit?.toString().toUpperCase();
        if (unitRaw === undefined || unitRaw.length === 0) {
            return null;
        }

        if (unitRaw === 'G') {
            return amount;
        }

        return null;
    }
}
