import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, EMPTY, expand, map, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { favoriteMealFromSdk } from '../../../shared/api/sdk/favorite-sdk.mapper';
import { FavoriteMealsSdk } from '../../../shared/api/sdk/generated/api/favorite-meals.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { FavoriteMeal } from '../../../shared/models/meal.data';
import type { PageOf } from '../../../shared/models/page-of.data';

const FAVORITE_PAGE_SIZE = 10;

const LOOKUP_PAGE_SIZE = 100;

@Service()
export class FavoriteMealService {
    protected readonly baseUrl = environment.apiUrls.favoriteMeals;
    private readonly sdk = createSdkConnection(FavoriteMealsSdk, this.baseUrl, inject(HttpClient));

    public getPage(page: number, limit = FAVORITE_PAGE_SIZE, search = ''): Observable<PageOf<FavoriteMeal>> {
        return this.sdk.client.getFavoriteMealsPage({ version: this.sdk.version, page, limit, search: search.trim() }).pipe(
            map(value => sdkPage(value, favoriteMealFromSdk)),
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
        return this.sdk.client
            .getFavoriteMealsCheckByMealId({ version: this.sdk.version, mealId })
            .pipe(catchError((error: unknown) => fallbackApiError('Check favorite meal error', error, false)));
    }

    public add(mealId: string, name?: string): Observable<FavoriteMeal> {
        return this.sdk.client.postFavoriteMeals({ version: this.sdk.version, addFavoriteMealHttpRequest: { mealId, name } }).pipe(
            map(favoriteMealFromSdk),
            catchError((error: unknown) => rethrowApiError('Add favorite meal error', error)),
        );
    }

    public restore(id: string): Observable<FavoriteMeal> {
        return this.sdk.client.postFavoriteMealsByIdRestore({ version: this.sdk.version, id }).pipe(
            map(favoriteMealFromSdk),
            catchError((error: unknown) => rethrowApiError('Restore favorite meal error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.sdk.client.deleteFavoriteMealsById({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Remove favorite meal error', error)),
        );
    }
}
