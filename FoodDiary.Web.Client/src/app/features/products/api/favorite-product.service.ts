import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, EMPTY, expand, map, type Observable, reduce } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { FavoriteProductsSdk } from '../../../shared/api/sdk/generated/api/favorite-products.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { FavoriteProduct } from '../../../shared/models/product.data';
import { favoriteProductFromSdk } from './product-sdk.mapper';

const FAVORITE_PAGE_SIZE = 10;

const LOOKUP_PAGE_SIZE = 100;

@Service()
export class FavoriteProductService {
    protected readonly baseUrl = environment.apiUrls.favoriteProducts;
    private readonly sdk = createSdkConnection(FavoriteProductsSdk, this.baseUrl, inject(HttpClient));

    public getPage(page: number, limit = FAVORITE_PAGE_SIZE, search = ''): Observable<PageOf<FavoriteProduct>> {
        return this.sdk.client.getFavoriteProductsPage({ version: this.sdk.version, page, limit, search: search.trim() }).pipe(
            map(result => sdkPage(result, favoriteProductFromSdk)),
            catchError((error: unknown) => rethrowApiError('Get favorite product page error', error)),
        );
    }

    public getLookupPage(): Observable<FavoriteProduct[]> {
        return this.getPage(1, LOOKUP_PAGE_SIZE).pipe(
            expand(page => (page.page < page.totalPages ? this.getPage(page.page + 1, LOOKUP_PAGE_SIZE) : EMPTY)),
            reduce((items, page) => [...items, ...page.data], [] as FavoriteProduct[]),
            catchError((error: unknown) => fallbackApiError('Get favorite products error', error, [])),
        );
    }

    public isFavorite(productId: string): Observable<boolean> {
        return this.sdk.client
            .getFavoriteProductsCheckByProductId({ version: this.sdk.version, productId })
            .pipe(catchError((error: unknown) => fallbackApiError('Check favorite product error', error, false)));
    }

    public add(productId: string, name?: string, preferredPortionAmount?: number): Observable<FavoriteProduct> {
        return this.sdk.client
            .postFavoriteProducts({ version: this.sdk.version, addFavoriteProductHttpRequest: { productId, name, preferredPortionAmount } })
            .pipe(
                map(favoriteProductFromSdk),
                catchError((error: unknown) => rethrowApiError('Add favorite product error', error)),
            );
    }

    public update(id: string, name: string | null, preferredPortionAmount: number): Observable<FavoriteProduct> {
        return this.sdk.client
            .putFavoriteProductsById({ version: this.sdk.version, id, updateFavoriteProductHttpRequest: { name, preferredPortionAmount } })
            .pipe(
                map(favoriteProductFromSdk),
                catchError((error: unknown) => rethrowApiError('Update favorite product error', error)),
            );
    }

    public remove(id: string): Observable<void> {
        return this.sdk.client.deleteFavoriteProductsById({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Remove favorite product error', error)),
        );
    }
}
