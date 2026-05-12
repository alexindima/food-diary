import { inject, Injectable } from '@angular/core';
import { catchError, map, type Observable, of } from 'rxjs';

import { RecipeLookupService } from '../../../shared/api/recipe-lookup.service';
import type { RecipeLookup, RecipeLookupIngredient } from '../../../shared/models/recipe-lookup.data';
import type { Recipe, RecipeIngredient } from '../../recipes/models/recipe.data';

@Injectable({
    providedIn: 'root',
})
export class RecipeServingWeightService {
    private readonly recipeLookupService = inject(RecipeLookupService);
    private readonly cache = new Map<string, number | null>();

    public loadServingWeight(recipe: Recipe | null): Observable<number | null> {
        if (recipe?.id === undefined || recipe.id.length === 0) {
            return of(null);
        }

        const cached = this.cache.get(recipe.id);
        if (cached !== undefined) {
            return of(cached);
        }

        const immediateWeight = this.calculateRecipeWeight(recipe);
        if (immediateWeight !== null && immediateWeight > 0 && recipe.servings > 0) {
            const servingWeight = immediateWeight / recipe.servings;
            this.cache.set(recipe.id, servingWeight);
            return of(servingWeight);
        }

        return this.recipeLookupService.getById(recipe.id).pipe(
            map(fullRecipe => {
                const computedWeight = this.calculateRecipeWeight(fullRecipe);
                if (computedWeight !== null && computedWeight > 0 && fullRecipe.servings > 0) {
                    const servingWeight = computedWeight / fullRecipe.servings;
                    this.cache.set(recipe.id, servingWeight);
                    return servingWeight;
                }
                this.cache.set(recipe.id, null);
                return null;
            }),
            catchError(() => {
                this.cache.set(recipe.id, null);
                return of(null);
            }),
        );
    }

    public convertServingsToGrams(recipe: Recipe | null, servingsAmount: number): number {
        const servingWeight = recipe?.id !== undefined && recipe.id.length > 0 ? this.cache.get(recipe.id) : null;
        if (servingWeight !== null && servingWeight !== undefined && servingWeight > 0) {
            return servingsAmount * servingWeight;
        }
        return servingsAmount;
    }

    public convertGramsToServings(recipe: Recipe | null, grams: number): number {
        const servingWeight = recipe?.id !== undefined && recipe.id.length > 0 ? this.cache.get(recipe.id) : null;
        if (servingWeight !== null && servingWeight !== undefined && servingWeight > 0) {
            return grams / servingWeight;
        }
        return grams;
    }

    private calculateRecipeWeight(recipe: Recipe | RecipeLookup): number | null {
        if (recipe.steps.length === 0) {
            return null;
        }

        let total = 0;
        recipe.steps.forEach(step => {
            step.ingredients.forEach(ingredient => {
                const weight = this.calculateIngredientWeight(ingredient);
                if (weight !== null && weight > 0) {
                    total += weight;
                }
            });
        });

        return total > 0 ? total : null;
    }

    private calculateIngredientWeight(ingredient: RecipeIngredient | RecipeLookupIngredient): number | null {
        const amount = ingredient.amount;
        if (amount <= 0) {
            return null;
        }

        const unitRaw = ingredient.productBaseUnit?.toString().toUpperCase();
        if (unitRaw === undefined || unitRaw.length === 0) {
            return null;
        }

        if (unitRaw === 'G' || unitRaw === 'ML') {
            return amount;
        }

        return null;
    }
}
