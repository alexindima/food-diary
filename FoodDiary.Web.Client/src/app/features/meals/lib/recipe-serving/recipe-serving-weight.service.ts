import { inject, Service } from '@angular/core';
import { catchError, defer, map, type Observable, of, tap } from 'rxjs';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import type { Recipe, RecipeIngredient } from '../../../../shared/models/recipe.data';
import type { RecipeLookup, RecipeLookupIngredient } from '../../../../shared/models/recipe-lookup.data';
import { servingMassFromObservation, type ServingMassResult, UNKNOWN_SERVING_MASS } from './recipe-display-amount';

@Service()
export class RecipeServingWeightService {
    private readonly recipeLookupService = inject(RecipeLookupService);
    private readonly cache = new Map<Recipe['id'], { recipeKey: string; mass: ServingMassResult; revision: number }>();
    private observationRevision = 0;

    public loadServingMass(recipe: Recipe): Observable<ServingMassResult> {
        if (recipe.id.length === 0) {
            return of(UNKNOWN_SERVING_MASS);
        }

        const recipeKey = this.recipeWeightKey(recipe);
        const cached = this.cache.get(recipe.id);
        if (cached?.recipeKey === recipeKey) {
            return of(cached.mass);
        }

        const immediateWeight = this.calculateRecipeWeight(recipe);
        if (immediateWeight !== null && immediateWeight > 0 && recipe.servings > 0) {
            const mass = servingMassFromObservation(immediateWeight / recipe.servings);
            this.storeServingMass(recipe.id, recipeKey, mass, ++this.observationRevision);
            return of(mass);
        }

        return defer(() => {
            const revision = ++this.observationRevision;
            return this.recipeLookupService.getById(recipe.id).pipe(
                map(fullRecipe => {
                    const computedWeight = this.calculateRecipeWeight(fullRecipe);
                    return computedWeight !== null && computedWeight > 0 && fullRecipe.servings > 0
                        ? servingMassFromObservation(computedWeight / fullRecipe.servings)
                        : UNKNOWN_SERVING_MASS;
                }),
                tap(mass => {
                    this.storeServingMass(recipe.id, recipeKey, mass, revision);
                }),
                catchError(() => of(UNKNOWN_SERVING_MASS)),
            );
        });
    }

    private storeServingMass(recipeId: Recipe['id'], recipeKey: string, mass: ServingMassResult, revision: number): void {
        const cached = this.cache.get(recipeId);
        if (cached !== undefined && cached.revision > revision) {
            return;
        }
        this.cache.set(recipeId, { recipeKey, mass, revision });
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
