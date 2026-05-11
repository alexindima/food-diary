import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { DailyMicronutrientSummary, UsdaFood, UsdaFoodDetail } from '../models/usda.data';

const DEFAULT_USDA_SEARCH_LIMIT = 20;

@Injectable({ providedIn: 'root' })
export class UsdaService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.usda;

    public searchFoods(search: string, limit: number = DEFAULT_USDA_SEARCH_LIMIT): Observable<UsdaFood[]> {
        return this.get<UsdaFood[]>('foods', { search, limit }).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Search USDA foods error', error, [])),
        );
    }

    public getFoodDetail(fdcId: number): Observable<UsdaFoodDetail> {
        return this.get<UsdaFoodDetail>(`foods/${fdcId}`).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Get USDA food detail error', error)),
        );
    }

    public linkProduct(productId: string, fdcId: number): Observable<void> {
        return this.put<void>(`products/${productId}/link`, { fdcId }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Link product to USDA food error', error)),
        );
    }

    public unlinkProduct(productId: string): Observable<void> {
        return this.delete<void>(`products/${productId}/link`).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Unlink product from USDA food error', error)),
        );
    }

    public getDailyMicronutrients(date: string): Observable<DailyMicronutrientSummary> {
        return this.get<DailyMicronutrientSummary>('daily-micronutrients', { date }).pipe(
            catchError((error: HttpErrorResponse) =>
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
