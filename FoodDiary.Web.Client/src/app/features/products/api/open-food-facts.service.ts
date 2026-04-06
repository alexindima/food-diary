import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';

export interface OpenFoodFactsProduct {
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
}

@Injectable({
    providedIn: 'root',
})
export class OpenFoodFactsService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.openFoodFacts;

    public searchByBarcode(barcode: string): Observable<OpenFoodFactsProduct | null> {
        return this.get<OpenFoodFactsProduct | null>(`products/${barcode}`).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Open Food Facts lookup error', error, null)),
        );
    }

    public search(query: string, limit = 10): Observable<OpenFoodFactsProduct[]> {
        return this.get<OpenFoodFactsProduct[]>('products', { search: query, limit }).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Open Food Facts search error', error, [])),
        );
    }
}
