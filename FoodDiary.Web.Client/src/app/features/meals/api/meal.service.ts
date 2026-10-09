import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { favoriteMealFromSdk } from '../../../shared/api/sdk/favorite-sdk.mapper';
import { type GetMealsRequestParams, MealsSdk } from '../../../shared/api/sdk/generated/api/meals.service';
import { mealFromSdk, mealRequestToSdk } from '../../../shared/api/sdk/meal-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkQueryString } from '../../../shared/api/sdk/sdk-query';
import { requireSdkFields, sdkPage } from '../../../shared/api/sdk/sdk-response';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { Meal, MealFilters, MealManageDto, MealOverview } from '../../../shared/models/meal.data';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { UtcInstant } from '../../../shared/models/semantics/date-value';
import type { MealId } from '../../../shared/models/semantics/entity-id';
import { MEAL_API_DEFAULT_FAVORITE_LIMIT } from './meal-api.config';

@Service()
export class MealService {
    protected readonly baseUrl = environment.apiUrls.meals;
    private readonly sdk = createSdkConnection(MealsSdk, this.baseUrl, inject(HttpClient));

    public query(page: number, limit: number, filters: MealFilters): Observable<PageOf<Meal>> {
        return this.sdk.client.getMeals({ ...this.mealFiltersToSdk(filters), page, limit }).pipe(
            map(pageData => sdkPage(pageData, response => mealFromSdk(response))),
            catchError((error: unknown) => rethrowApiError('Query meals error', error)),
        );
    }

    public queryOverview(
        page: number,
        limit: number,
        filters: MealFilters,
        favorites: { limit?: number; include?: boolean } = {},
    ): Observable<MealOverview> {
        return this.sdk.client
            .getMealsOverview({
                ...this.mealFiltersToSdk(filters),
                page,
                limit,
                favoriteLimit: favorites.limit ?? MEAL_API_DEFAULT_FAVORITE_LIMIT,
                includeFavorites: favorites.include ?? true,
                timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
                timeZoneOffsetMinutes: -new Date().getTimezoneOffset(),
            })
            .pipe(
                map(response => {
                    const value = requireSdkFields(response, ['allMeals', 'favoriteItems', 'favoriteTotalCount']);
                    return {
                        allMeals: sdkPage(value.allMeals, item => mealFromSdk(item)),
                        daySummaries: value.daySummaries?.map(day => requireSdkFields(day, ['date', 'totalCalories', 'mealCount'])) ?? [],
                        favoriteItems: value.favoriteItems.map(favoriteMealFromSdk),
                        favoriteTotalCount: value.favoriteTotalCount,
                    };
                }),
                catchError((error: unknown) => rethrowApiError('Query meal overview error', error)),
            );
    }

    public getById(id: MealId): Observable<Meal | null> {
        return this.sdk.client.getMealsById({ version: this.sdk.version, id }).pipe(
            map(response => mealFromSdk(response)),
            catchError(() => of(null)),
        );
    }

    public create(data: MealManageDto): Observable<Meal> {
        return this.sdk.client.postMeals({ version: this.sdk.version, createMealHttpRequest: mealRequestToSdk(data) }).pipe(
            map(response => mealFromSdk(response)),
            catchError((error: unknown) => rethrowApiError('Create meal error', error)),
        );
    }

    public update(id: MealId, data: MealManageDto): Observable<Meal> {
        return this.sdk.client.patchMealsById({ version: this.sdk.version, id, updateMealHttpRequest: mealRequestToSdk(data) }).pipe(
            map(response => mealFromSdk(response)),
            catchError((error: unknown) => rethrowApiError('Update meal error', error)),
        );
    }

    public deleteById(id: MealId): Observable<void> {
        return this.sdk.client
            .deleteMealsById({ version: this.sdk.version, id })
            .pipe(catchError((error: unknown) => rethrowApiError('Delete meal error', error)));
    }

    public repeat(id: MealId, targetDate: UtcInstant, mealType?: string): Observable<Meal> {
        return this.sdk.client.postMealsByIdRepeat({ version: this.sdk.version, id, repeatMealHttpRequest: { targetDate, mealType } }).pipe(
            map(response => mealFromSdk(response)),
            catchError((error: unknown) => rethrowApiError('Repeat meal error', error)),
        );
    }

    private mealFiltersToSdk(filters: MealFilters): GetMealsRequestParams {
        return { version: this.sdk.version, ...filters, mealTypes: sdkQueryString(filters.mealTypes) };
    }
}
