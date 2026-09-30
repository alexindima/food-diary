import { Service } from '@angular/core';
import { catchError, EMPTY, expand, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { FavoriteRecipe } from '../models/recipe.data';

const FAVORITE_PAGE_SIZE = 10;

const LOOKUP_PAGE_SIZE = 100;

@Service()
export class FavoriteRecipeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.favoriteRecipes;

    public getPage(page: number, limit = FAVORITE_PAGE_SIZE, search = ''): Observable<PageOf<FavoriteRecipe>> {
        return this.get<PageOf<FavoriteRecipe>>('page', { page, limit, search: search.trim() }).pipe(
            catchError((error: unknown) => rethrowApiError('Get favorite recipe page error', error)),
        );
    }

    public getLookupPage(): Observable<FavoriteRecipe[]> {
        return this.getPage(1, LOOKUP_PAGE_SIZE).pipe(
            expand(page => (page.page < page.totalPages ? this.getPage(page.page + 1, LOOKUP_PAGE_SIZE) : EMPTY)),
            reduce((items, page) => [...items, ...page.data], [] as FavoriteRecipe[]),
            catchError((error: unknown) => fallbackApiError('Get favorite recipes error', error, [])),
        );
    }

    public isFavorite(recipeId: string): Observable<boolean> {
        return this.get<boolean>(`check/${recipeId}`).pipe(
            catchError((error: unknown) => fallbackApiError('Check favorite recipe error', error, false)),
        );
    }

    public add(recipeId: string, name?: string): Observable<FavoriteRecipe> {
        return this.post<FavoriteRecipe>('', { recipeId, name }).pipe(
            catchError((error: unknown) => rethrowApiError('Add favorite recipe error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Remove favorite recipe error', error)));
    }
}
