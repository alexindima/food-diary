import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { UsdaSdk } from '../../../shared/api/sdk/generated/api/usda.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { DailyMicronutrientSummary, UsdaFood, UsdaFoodDetail } from '../../../shared/models/usda.data';
import { USDA_SEARCH_LIMIT } from './usda-api.tokens';
import { dailyMicronutrientsFromSdk, usdaDetailFromSdk } from './usda-sdk.mapper';

@Service()
export class UsdaService {
    private readonly defaultSearchLimit = inject(USDA_SEARCH_LIMIT);

    protected readonly baseUrl = environment.apiUrls.usda;
    private readonly sdk = createSdkConnection(UsdaSdk, this.baseUrl, inject(HttpClient));

    public searchFoods(search: string, limit?: number): Observable<UsdaFood[]> {
        return this.sdk.client.getUsdaFoods({ version: this.sdk.version, search, limit: limit ?? this.defaultSearchLimit }).pipe(
            map(values =>
                values.map(response => ({
                    ...requireSdkFields(response, ['fdcId', 'description']),
                    foodCategory: response.foodCategory ?? null,
                })),
            ),
            catchError((error: unknown) => fallbackApiError('Search USDA foods error', error, [])),
        );
    }

    public getFoodDetail(fdcId: number): Observable<UsdaFoodDetail> {
        return this.sdk.client.getUsdaFoodsByFdcId({ version: this.sdk.version, fdcId }).pipe(
            map(usdaDetailFromSdk),
            catchError((error: unknown) => rethrowApiError('Get USDA food detail error', error)),
        );
    }

    public linkProduct(productId: string, fdcId: number): Observable<void> {
        return this.sdk.client
            .putUsdaProductsByProductIdLink({ version: this.sdk.version, productId, linkProductToUsdaFoodHttpRequest: { fdcId } })
            .pipe(
                map(() => {}),
                catchError((error: unknown) => rethrowApiError('Link product to USDA food error', error)),
            );
    }

    public unlinkProduct(productId: string): Observable<void> {
        return this.sdk.client.deleteUsdaProductsByProductIdLink({ version: this.sdk.version, productId }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Unlink product from USDA food error', error)),
        );
    }

    public getDailyMicronutrients(date: string): Observable<DailyMicronutrientSummary> {
        return this.sdk.client.getUsdaDailyMicronutrients({ version: this.sdk.version, date }).pipe(
            map(dailyMicronutrientsFromSdk),
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
import { HttpClient } from '@angular/common/http';
