import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { PageOf } from '../../../shared/models/page-of.data';
import { RecipeService } from '../api/recipe.service';
import type { Recipe, RecipeFilters } from '../models/recipe.data';

@Injectable({ providedIn: 'root' })
export class RecipeSelectFacade {
    private readonly recipeService = inject(RecipeService);

    public query(page: number, limit: number, filters?: RecipeFilters, includePublic = true): Observable<PageOf<Recipe>> {
        return this.recipeService.query(page, limit, filters, includePublic);
    }
}
