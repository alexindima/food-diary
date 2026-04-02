import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { PageOf } from '../../../shared/models/page-of.data';
import { Recipe, RecipeDto, RecipeFilters, RecipeListWithRecent } from '../models/recipe.data';

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
        if (search) {
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
        return this.get<Recipe>(`${id}`, params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get recipe error', error, null)),
        );
    }

    public getRecent(limit = 10, includePublic = true): Observable<Recipe[]> {
        const params: Record<string, string | number | boolean> = { limit, includePublic };
        return this.get<Recipe[]>('recent', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get recent recipes error', error, [])),
        );
    }

    public queryWithRecent(
        page: number,
        limit: number,
        filters?: RecipeFilters,
        includePublic = true,
        recentLimit = 10,
    ): Observable<RecipeListWithRecent> {
        const params: Record<string, string | number | boolean> = { page, limit, includePublic, recentLimit };
        const search = filters?.search?.trim();
        if (search) {
            params['search'] = search;
        }

        return this.get<RecipeListWithRecent>('with-recent', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Query recipes with recent error', error, {
                    recentItems: [],
                    allRecipes: { data: [], page, limit, totalPages: 0, totalItems: 0 },
                }),
            ),
        );
    }

    public create(data: RecipeDto): Observable<Recipe> {
        return this.post<Recipe>('', data).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Create recipe error', error)));
    }

    public update(id: string, data: RecipeDto): Observable<Recipe> {
        return this.patch<Recipe>(`${id}`, data).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Update recipe error', error)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(`${id}`).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Delete recipe error', error)));
    }

    public duplicate(id: string): Observable<Recipe> {
        return this.post<Recipe>(`${id}/duplicate`, {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Duplicate recipe error', error)),
        );
    }
}
