import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { ApiService } from '../../../services/api.service';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { PublicRecipe, PublicRecipeFilters } from '../models/public-recipe.data';
import { PUBLIC_RECIPE_API_URL } from './public-recipe-api.token';

@Service()
export class PublicRecipeService extends ApiService {
    protected readonly baseUrl = inject(PUBLIC_RECIPE_API_URL);

    public query(filters: PublicRecipeFilters): Observable<PageOf<PublicRecipe>> {
        return this.get<PageOf<PublicRecipe>>('', { ...filters, limit: 20 });
    }

    public getById(id: string): Observable<PublicRecipe> {
        return this.get<PublicRecipe>(encodeURIComponent(id));
    }
}
