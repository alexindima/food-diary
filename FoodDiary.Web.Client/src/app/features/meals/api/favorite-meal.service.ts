import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { FavoriteMeal } from '../models/meal.data';

@Injectable({ providedIn: 'root' })
export class FavoriteMealService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.favoriteMeals;

    public getAll(): Observable<FavoriteMeal[]> {
        return this.get<FavoriteMeal[]>('').pipe(catchError((error: unknown) => fallbackApiError('Get favorite meals error', error, [])));
    }

    public isFavorite(mealId: string): Observable<boolean> {
        return this.get<boolean>(`check/${mealId}`).pipe(
            catchError((error: unknown) => fallbackApiError('Check favorite meal error', error, false)),
        );
    }

    public add(mealId: string, name?: string): Observable<FavoriteMeal> {
        return this.post<FavoriteMeal>('', { mealId, name }).pipe(
            catchError((error: unknown) => rethrowApiError('Add favorite meal error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Remove favorite meal error', error)));
    }
}
