import { inject, Injectable } from '@angular/core';
import { firstValueFrom, type Observable } from 'rxjs';

import type { PageOf } from '../../../shared/models/page-of.data';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { PublicRecipeService } from '../api/public-recipe.service';
import { RecipeService } from '../api/recipe.service';
import type { PublicRecipe, PublicRecipeFilters } from '../models/public-recipe.data';

@Injectable()
export class PublicRecipesFacade {
    private readonly api = inject(PublicRecipeService);
    private readonly recipes = inject(RecipeService);
    private readonly favorites = inject(FavoriteRecipeService);
    private readonly quickMeal = inject(QuickMealService);

    public query(filters: PublicRecipeFilters): Observable<PageOf<PublicRecipe>> {
        return this.api.query(filters);
    }

    public getCategories(search: string, language?: string): Observable<string[]> {
        return this.api.getCategories(search, language);
    }

    public isFavorite(id: string): Observable<boolean> {
        return this.favorites.isFavorite(id);
    }

    public async saveAsync(recipe: PublicRecipe): Promise<void> {
        await firstValueFrom(this.favorites.add(recipe.id, recipe.name));
    }

    public async addToDiaryAsync(id: string): Promise<void> {
        const recipe = await firstValueFrom(this.recipes.getById(id));
        if (recipe === null) {
            throw new Error('Recipe unavailable');
        }
        this.quickMeal.addRecipe(recipe);
    }
}
