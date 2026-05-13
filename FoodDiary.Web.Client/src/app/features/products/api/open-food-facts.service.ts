import { inject, Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { OPEN_FOOD_FACTS_SEARCH_LIMIT } from './product-api.tokens';

export type OpenFoodFactsProduct = {
    barcode: string;
    name: string;
    brand?: string | null;
    category?: string | null;
    imageUrl?: string | null;
    caloriesPer100G?: number | null;
    proteinsPer100G?: number | null;
    fatsPer100G?: number | null;
    carbsPer100G?: number | null;
    fiberPer100G?: number | null;
};

@Injectable({ providedIn: 'root' })
export class OpenFoodFactsService extends ApiService {
    private readonly defaultSearchLimit = inject(OPEN_FOOD_FACTS_SEARCH_LIMIT);

    protected readonly baseUrl = environment.apiUrls.openFoodFacts;

    public searchByBarcode(barcode: string): Observable<OpenFoodFactsProduct | null> {
        return this.get<OpenFoodFactsProduct | null>(`products/${barcode}`).pipe(
            catchError((error: unknown) => fallbackApiError('Open Food Facts lookup error', error, null)),
        );
    }

    public search(query: string, limit?: number): Observable<OpenFoodFactsProduct[]> {
        return this.get<OpenFoodFactsProduct[]>('products', { search: query, limit: limit ?? this.defaultSearchLimit }).pipe(
            catchError((error: unknown) => fallbackApiError('Open Food Facts search error', error, [])),
        );
    }
}
