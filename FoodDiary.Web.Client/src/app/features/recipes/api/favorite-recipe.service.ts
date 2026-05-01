import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { FavoriteRecipe } from '../models/recipe.data';

@Injectable({
    providedIn: 'root',
})
export class FavoriteRecipeService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.favoriteRecipes;

    public getAll(): Observable<FavoriteRecipe[]> {
        return this.get<FavoriteRecipe[]>('').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get favorite recipes error', error, [])),
        );
    }

    public isFavorite(recipeId: string): Observable<boolean> {
        return this.get<boolean>(`check/${recipeId}`).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Check favorite recipe error', error, false)),
        );
    }

    public add(recipeId: string, name?: string): Observable<FavoriteRecipe> {
        return this.post<FavoriteRecipe>('', { recipeId, name }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Add favorite recipe error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Remove favorite recipe error', error)));
    }
}
