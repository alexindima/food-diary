import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { OpenFoodFactsSdk } from '../../../shared/api/sdk/generated/api/open-food-facts.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields, sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import type { OpenFoodFactsProduct } from '../models/open-food-facts.data';
import { OPEN_FOOD_FACTS_SEARCH_LIMIT } from './product-api.tokens';

@Service()
export class OpenFoodFactsService {
    private readonly defaultSearchLimit = inject(OPEN_FOOD_FACTS_SEARCH_LIMIT);

    protected readonly baseUrl = environment.apiUrls.openFoodFacts;
    private readonly sdk = createSdkConnection(OpenFoodFactsSdk, this.baseUrl, inject(HttpClient));

    public searchByBarcode(barcode: string): Observable<OpenFoodFactsProduct | null> {
        return this.sdk.client.getOpenFoodFactsProductsByBarcode({ version: this.sdk.version, barcode }).pipe(
            map(value => sdkOptional(value, item => requireSdkFields(item, ['barcode', 'name']))),
            catchError((error: unknown) => fallbackApiError('Open Food Facts lookup error', error, null)),
        );
    }

    public search(query: string, limit?: number): Observable<OpenFoodFactsProduct[]> {
        return this.sdk.client
            .getOpenFoodFactsProducts({ version: this.sdk.version, search: query, limit: limit ?? this.defaultSearchLimit })
            .pipe(
                map(values => values.map(value => requireSdkFields(value, ['barcode', 'name']))),
                catchError((error: unknown) => fallbackApiError('Open Food Facts search error', error, [])),
            );
    }
}
import { HttpClient } from '@angular/common/http';
