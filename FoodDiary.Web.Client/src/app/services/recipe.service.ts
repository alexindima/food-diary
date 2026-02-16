import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { catchError, Observable, throwError } from 'rxjs';
import { PageOf } from '../types/page-of.data';
import { Recipe, RecipeDto, RecipeFilters } from '../types/recipe.data';
import { ApiService } from './api.service';
import { HttpErrorResponse } from '@angular/common/http';

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
            catchError((error: HttpErrorResponse) => {
                console.error('Query recipes error', error);
                return throwError(() => error);
            }),
        );
    }

    public getById(id: string, includePublic = true): Observable<Recipe> {
        const params = { includePublic };
        return this.get<Recipe>(`${id}`, params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Get recipe error', error);
                return throwError(() => error);
            }),
        );
    }

    public getRecent(limit = 10, includePublic = true): Observable<Recipe[]> {
        const params: Record<string, string | number | boolean> = { limit, includePublic };
        return this.get<Recipe[]>('recent', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Get recent recipes error', error);
                return throwError(() => error);
            }),
        );
    }

    public create(data: RecipeDto): Observable<Recipe> {
        return this.post<Recipe>('', data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Create recipe error', error);
                return throwError(() => error);
            }),
        );
    }

    public update(id: string, data: RecipeDto): Observable<Recipe> {
        return this.patch<Recipe>(`${id}`, data).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Update recipe error', error);
                return throwError(() => error);
            }),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Delete recipe error', error);
                return throwError(() => error);
            }),
        );
    }

    public duplicate(id: string): Observable<Recipe> {
        return this.post<Recipe>(`${id}/duplicate`, {}).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Duplicate recipe error', error);
                return throwError(() => error);
            }),
        );
    }
}
