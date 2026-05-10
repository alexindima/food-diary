import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { Recipe, RecipeDto, RecipeFilters, RecipeOverview } from '../models/recipe.data';

export interface RecipeOverviewQuery {
    page: number;
    limit: number;
    filters?: RecipeFilters;
    includePublic?: boolean;
    recentLimit?: number;
    favoriteLimit?: number;
}

const DEFAULT_RECENT_RECIPE_LIMIT = 10;
const DEFAULT_RECIPE_OVERVIEW_RECENT_LIMIT = 10;
const DEFAULT_RECIPE_OVERVIEW_FAVORITE_LIMIT = 10;

@Injectable({
    providedIn: 'root',
})
export class RecipeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.recipes;

    public query(page: number, limit: number, filters?: RecipeFilters, includePublic = true): Observable<PageOf<Recipe>> {
        const params: Record<string, string | number | boolean> = {
            page,
            limit,
            includePublic,
        };

        const search = filters?.search?.trim();
        if (search !== undefined && search.length > 0) {
            params['search'] = search;
        }

        return this.get<PageOf<Recipe>>('', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Query recipes error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public getById(id: string, includePublic = true): Observable<Recipe | null> {
        const params = { includePublic };
        return this.get<Recipe>(id, params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get recipe error', error, null)),
        );
    }

    public getRecent(limit = DEFAULT_RECENT_RECIPE_LIMIT, includePublic = true): Observable<Recipe[]> {
        const params: Record<string, string | number | boolean> = { limit, includePublic };
        return this.get<Recipe[]>('recent', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get recent recipes error', error, [])),
        );
    }

    public queryOverview(query: RecipeOverviewQuery): Observable<RecipeOverview> {
        const {
            page,
            limit,
            filters,
            includePublic = true,
            recentLimit = DEFAULT_RECIPE_OVERVIEW_RECENT_LIMIT,
            favoriteLimit = DEFAULT_RECIPE_OVERVIEW_FAVORITE_LIMIT,
        } = query;
        const params: Record<string, string | number | boolean> = { page, limit, includePublic, recentLimit, favoriteLimit };
        const search = filters?.search?.trim();
        if (search !== undefined && search.length > 0) {
            params['search'] = search;
        }

        return this.get<RecipeOverview>('overview', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Query recipe overview error', error, {
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                    allRecipes: { data: [], page, limit, totalPages: 0, totalItems: 0 },
                }),
            ),
        );
    }

    public create(data: RecipeDto): Observable<Recipe> {
        return this.post<Recipe>('', data).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Create recipe error', error)));
    }

    public update(id: string, data: RecipeDto): Observable<Recipe> {
        return this.patch<Recipe>(id, data).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Update recipe error', error)));
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Delete recipe error', error)));
    }

    public duplicate(id: string): Observable<Recipe> {
        return this.post<Recipe>(`${id}/duplicate`, {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Duplicate recipe error', error)),
        );
    }
}
