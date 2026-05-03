import { type HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { type FavoriteProduct } from '../models/product.data';

@Injectable({
    providedIn: 'root',
})
export class FavoriteProductService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.favoriteProducts;

    public getAll(): Observable<FavoriteProduct[]> {
        return this.get<FavoriteProduct[]>('').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get favorite products error', error, [])),
        );
    }

    public isFavorite(productId: string): Observable<boolean> {
        return this.get<boolean>(`check/${productId}`).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Check favorite product error', error, false)),
        );
    }

    public add(productId: string, name?: string): Observable<FavoriteProduct> {
        return this.post<FavoriteProduct>('', { productId, name }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Add favorite product error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.delete<void>(id).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Remove favorite product error', error)),
        );
    }
}
