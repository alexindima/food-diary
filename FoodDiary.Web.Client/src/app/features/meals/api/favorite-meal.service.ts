import { Service } from '@angular/core';
import { catchError, EMPTY, expand, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { FavoriteMeal } from '../../../shared/models/meal.data';
import type { PageOf } from '../../../shared/models/page-of.data';

const FAVORITE_PAGE_SIZE = 10;

const LOOKUP_PAGE_SIZE = 100;

@Service()
export class FavoriteMealService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.favoriteMeals;

    public getPage(page: number, limit = FAVORITE_PAGE_SIZE, search = ''): Observable<PageOf<FavoriteMeal>> {
        return this.get<PageOf<FavoriteMeal>>('page', { page, limit, search: search.trim() }).pipe(
            catchError((error: unknown) => rethrowApiError('Get favorite meal page error', error)),
        );
    }

    public getLookupPage(): Observable<FavoriteMeal[]> {
        return this.getPage(1, LOOKUP_PAGE_SIZE).pipe(
            expand(page => (page.page < page.totalPages ? this.getPage(page.page + 1, LOOKUP_PAGE_SIZE) : EMPTY)),
            reduce((items, page) => [...items, ...page.data], [] as FavoriteMeal[]),
            catchError((error: unknown) => fallbackApiError('Get favorite meals error', error, [])),
        );
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

    public restore(id: string): Observable<FavoriteMeal> {
        return this.post<FavoriteMeal>(`${id}/restore`, {}).pipe(
            catchError((error: unknown) => rethrowApiError('Restore favorite meal error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Remove favorite meal error', error)));
    }
}
