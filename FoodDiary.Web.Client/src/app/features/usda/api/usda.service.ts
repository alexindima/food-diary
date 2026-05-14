import { inject, Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { DailyMicronutrientSummary, UsdaFood, UsdaFoodDetail } from '../models/usda.data';
import { USDA_SEARCH_LIMIT } from './usda-api.tokens';

@Injectable({ providedIn: 'root' })
export class UsdaService extends ApiService {
    private readonly defaultSearchLimit = inject(USDA_SEARCH_LIMIT);

    protected readonly baseUrl = environment.apiUrls.usda;

    public searchFoods(search: string, limit?: number): Observable<UsdaFood[]> {
        return this.get<UsdaFood[]>('foods', { search, limit: limit ?? this.defaultSearchLimit }).pipe(
            catchError((error: unknown) => fallbackApiError('Search USDA foods error', error, [])),
        );
    }

    public getFoodDetail(fdcId: number): Observable<UsdaFoodDetail> {
        return this.get<UsdaFoodDetail>(`foods/${fdcId}`).pipe(
            catchError((error: unknown) => rethrowApiError('Get USDA food detail error', error)),
        );
    }

    public linkProduct(productId: string, fdcId: number): Observable<void> {
        return this.put<void>(`products/${productId}/link`, { fdcId }).pipe(
            catchError((error: unknown) => rethrowApiError('Link product to USDA food error', error)),
        );
    }

    public unlinkProduct(productId: string): Observable<void> {
        return this.delete<void>(`products/${productId}/link`).pipe(
            catchError((error: unknown) => rethrowApiError('Unlink product from USDA food error', error)),
        );
    }

    public getDailyMicronutrients(date: string): Observable<DailyMicronutrientSummary> {
        return this.get<DailyMicronutrientSummary>('daily-micronutrients', { date }).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Get daily micronutrients error', error, {
                    date,
                    linkedProductCount: 0,
                    totalProductCount: 0,
                    nutrients: [],
                    healthScores: null,
                }),
            ),
        );
    }
}
