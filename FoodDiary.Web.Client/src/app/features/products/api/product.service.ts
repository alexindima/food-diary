import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { PageOf } from '../../../shared/models/page-of.data';
import { CreateProductRequest, Product, ProductFilters, ProductOverview, UpdateProductRequest } from '../models/product.data';

@Injectable({
    providedIn: 'root',
})
export class ProductService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.products;

    public query(page: number, limit: number, filters?: ProductFilters, includePublic = true): Observable<PageOf<Product>> {
        const params: Record<string, string | number | boolean> = { page, limit, includePublic };
        const search = filters?.search?.trim();
        if (search) {
            params['search'] = search;
        }
        if (filters?.productTypes && filters.productTypes.length > 0) {
            params['productTypes'] = filters.productTypes.join(',');
        }

        return this.get<PageOf<Product>>('', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Query products error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public getById(id: string): Observable<Product | null> {
        return this.get<Product>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get product error', error, null)),
        );
    }

    public queryOverview(
        page: number,
        limit: number,
        filters?: ProductFilters,
        includePublic = true,
        recentLimit = 10,
        favoriteLimit = 10,
    ): Observable<ProductOverview> {
        const params: Record<string, string | number | boolean> = { page, limit, includePublic, recentLimit, favoriteLimit };
        const search = filters?.search?.trim();
        if (search) {
            params['search'] = search;
        }
        if (filters?.productTypes && filters.productTypes.length > 0) {
            params['productTypes'] = filters.productTypes.join(',');
        }

        return this.get<ProductOverview>('overview', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Query product overview error', error, {
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                    allProducts: { data: [], page, limit, totalPages: 0, totalItems: 0 },
                }),
            ),
        );
    }

    public getRecent(limit = 10, includePublic = true): Observable<Product[]> {
        const params: Record<string, string | number | boolean> = { limit, includePublic };
        return this.get<Product[]>('recent', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get recent products error', error, [])),
        );
    }

    public create(data: CreateProductRequest): Observable<Product> {
        return this.post<Product>('', data).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Create product error', error)));
    }

    public update(id: string, data: UpdateProductRequest): Observable<Product> {
        return this.patch<Product>(`${id}`, data).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Update product error', error)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(`${id}`).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Delete product error', error)));
    }

    public duplicate(id: string): Observable<Product> {
        return this.post<Product>(`${id}/duplicate`, {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Duplicate product error', error)),
        );
    }
}
