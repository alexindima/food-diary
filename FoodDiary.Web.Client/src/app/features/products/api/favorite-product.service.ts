import { Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { FavoriteProduct } from '../models/product.data';
import { normalizeProductUnit } from './product-unit.mapper';

const FAVORITE_PAGE_SIZE = 10;

@Service()
export class FavoriteProductService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.favoriteProducts;

    public getPage(page: number, limit = FAVORITE_PAGE_SIZE, search = ''): Observable<PageOf<FavoriteProduct>> {
        return this.get<PageOf<FavoriteProduct>>('page', { page, limit, search: search.trim() }).pipe(
            map(result => ({ ...result, data: result.data.map(normalizeProductUnit) })),
            catchError((error: unknown) => rethrowApiError('Get favorite product page error', error)),
        );
    }

    public getAll(): Observable<FavoriteProduct[]> {
        return this.get<FavoriteProduct[]>('').pipe(
            map(products => products.map(normalizeProductUnit)),
            catchError((error: unknown) => fallbackApiError('Get favorite products error', error, [])),
        );
    }

    public isFavorite(productId: string): Observable<boolean> {
        return this.get<boolean>(`check/${productId}`).pipe(
            catchError((error: unknown) => fallbackApiError('Check favorite product error', error, false)),
        );
    }

    public add(productId: string, name?: string, preferredPortionAmount?: number): Observable<FavoriteProduct> {
        return this.post<FavoriteProduct>('', { productId, name, preferredPortionAmount }).pipe(
            map(normalizeProductUnit),
            catchError((error: unknown) => rethrowApiError('Add favorite product error', error)),
        );
    }

    public update(id: string, name: string | null, preferredPortionAmount: number): Observable<FavoriteProduct> {
        return this.put<FavoriteProduct>(id, { name, preferredPortionAmount }).pipe(
            map(normalizeProductUnit),
            catchError((error: unknown) => rethrowApiError('Update favorite product error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Remove favorite product error', error)));
    }
}
