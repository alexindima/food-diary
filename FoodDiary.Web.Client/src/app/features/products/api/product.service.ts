import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type {
    CreateProductRequest,
    Product,
    ProductFilters,
    ProductOverview,
    ProductSearchSuggestion,
    UpdateProductRequest,
} from '../models/product.data';

const DEFAULT_RECENT_LIMIT = 10;
const DEFAULT_FAVORITE_LIMIT = 10;
const DEFAULT_SUGGESTIONS_LIMIT = 5;

export type ProductOverviewQuery = {
    page: number;
    limit: number;
    filters?: ProductFilters;
    includePublic?: boolean;
    recentLimit?: number;
    favoriteLimit?: number;
};

@Injectable({
    providedIn: 'root',
})
export class ProductService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.products;

    public query(page: number, limit: number, filters?: ProductFilters, includePublic = true): Observable<PageOf<Product>> {
        const params: Record<string, string | number | boolean> = { page, limit, includePublic };
        this.applyProductFilters(params, filters);

        return this.get<PageOf<Product>>('', params).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Query products error', error, { data: [], page, limit, totalPages: 0, totalItems: 0 }),
            ),
        );
    }

    public getById(id: string): Observable<Product | null> {
        return this.get<Product>(id).pipe(catchError((error: unknown) => fallbackApiError('Get product error', error, null)));
    }

    public queryOverview(query: ProductOverviewQuery): Observable<ProductOverview> {
        const {
            page,
            limit,
            filters,
            includePublic = true,
            recentLimit = DEFAULT_RECENT_LIMIT,
            favoriteLimit = DEFAULT_FAVORITE_LIMIT,
        } = query;
        const params: Record<string, string | number | boolean> = { page, limit, includePublic, recentLimit, favoriteLimit };
        this.applyProductFilters(params, filters);

        return this.get<ProductOverview>('overview', params).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Query product overview error', error, {
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                    allProducts: { data: [], page, limit, totalPages: 0, totalItems: 0 },
                }),
            ),
        );
    }

    private applyProductFilters(params: Record<string, string | number | boolean>, filters?: ProductFilters): void {
        const search = filters?.search?.trim();
        if (search !== undefined && search.length > 0) {
            params['search'] = search;
        }
        if (filters?.productTypes !== undefined && filters.productTypes.length > 0) {
            params['productTypes'] = filters.productTypes.join(',');
        }
    }

    public getRecent(limit = DEFAULT_RECENT_LIMIT, includePublic = true): Observable<Product[]> {
        const params: Record<string, string | number | boolean> = { limit, includePublic };
        return this.get<Product[]>('recent', params).pipe(
            catchError((error: unknown) => fallbackApiError('Get recent products error', error, [])),
        );
    }

    public searchSuggestions(search: string, limit = DEFAULT_SUGGESTIONS_LIMIT): Observable<ProductSearchSuggestion[]> {
        return this.get<ProductSearchSuggestion[]>('suggestions', { search, limit }).pipe(
            catchError((error: unknown) => fallbackApiError('Search product suggestions error', error, [])),
        );
    }

    public create(data: CreateProductRequest): Observable<Product> {
        return this.post<Product>('', data).pipe(catchError((error: unknown) => rethrowApiError('Create product error', error)));
    }

    public update(id: string, data: UpdateProductRequest): Observable<Product> {
        return this.patch<Product>(id, data).pipe(catchError((error: unknown) => rethrowApiError('Update product error', error)));
    }

    public deleteById(id: string): Observable<void> {
        return this.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Delete product error', error)));
    }

    public duplicate(id: string): Observable<Product> {
        return this.post<Product>(`${id}/duplicate`, {}).pipe(
            catchError((error: unknown) => rethrowApiError('Duplicate product error', error)),
        );
    }
}
