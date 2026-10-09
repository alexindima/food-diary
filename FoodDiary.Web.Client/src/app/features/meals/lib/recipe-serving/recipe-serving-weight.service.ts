import { inject, Service } from '@angular/core';
import { catchError, map, type Observable, of } from 'rxjs';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import type { Recipe, RecipeIngredient } from '../../../../shared/models/recipe.data';
import type { RecipeLookup, RecipeLookupIngredient } from '../../../../shared/models/recipe-lookup.data';
import type { RecipeServings } from '../../../../shared/models/semantics/meal-quantity';
import {
    displayRecipeServings,
    type RecipeDisplayAmount,
    recipeDisplayFromInput,
    servingMassFromObservation,
    type ServingMassResult,
    UNKNOWN_SERVING_MASS,
} from './recipe-display-amount';

@Service()
export class RecipeServingWeightService {
    private readonly recipeLookupService = inject(RecipeLookupService);
    private readonly cache = new Map<string, { recipeKey: string; mass: ServingMassResult }>();

    public loadServingMass(recipe: Recipe | null): Observable<ServingMassResult> {
        if (recipe?.id === undefined || recipe.id.length === 0) {
            return of(UNKNOWN_SERVING_MASS);
        }

        const cached = this.cache.get(recipe.id);
        if (cached?.recipeKey === this.recipeWeightKey(recipe)) {
            return of(cached.mass);
        }

        const immediateWeight = this.calculateRecipeWeight(recipe);
        if (immediateWeight !== null && immediateWeight > 0 && recipe.servings > 0) {
            const mass = servingMassFromObservation(immediateWeight / recipe.servings);
            this.storeServingMass(recipe, mass);
            return of(mass);
        }

        return this.recipeLookupService.getById(recipe.id).pipe(
            map(fullRecipe => {
                const computedWeight = this.calculateRecipeWeight(fullRecipe);
                if (computedWeight !== null && computedWeight > 0 && fullRecipe.servings > 0) {
                    const mass = servingMassFromObservation(computedWeight / fullRecipe.servings);
                    this.storeServingMass(recipe, mass);
                    return mass;
                }
                this.storeServingMass(recipe, UNKNOWN_SERVING_MASS);
                return UNKNOWN_SERVING_MASS;
            }),
            catchError(() => {
                this.storeServingMass(recipe, UNKNOWN_SERVING_MASS);
                return of(UNKNOWN_SERVING_MASS);
            }),
        );
    }

    public displayServings(recipe: Recipe | null, servings: RecipeServings): RecipeDisplayAmount {
        return displayRecipeServings(servings, this.cachedServingMass(recipe));
    }

    public displayAmountFromInput(recipe: Recipe | null, amount: number): RecipeDisplayAmount {
        return recipeDisplayFromInput(amount, this.cachedServingMass(recipe));
    }

    public hasServingWeight(recipe: Recipe | null): boolean {
        return this.cachedServingMass(recipe).kind === 'known';
    }

    private cachedServingMass(recipe: Recipe | null): ServingMassResult {
        if (recipe === null) {
            return UNKNOWN_SERVING_MASS;
        }
        const cached = this.cache.get(recipe.id);
        return cached?.recipeKey === this.recipeWeightKey(recipe) ? cached.mass : UNKNOWN_SERVING_MASS;
    }

    private storeServingMass(recipe: Recipe, mass: ServingMassResult): void {
        this.cache.set(recipe.id, { recipeKey: this.recipeWeightKey(recipe), mass });
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
