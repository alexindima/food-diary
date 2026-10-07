import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { QueryProductsRequestParams } from '../../../shared/api/sdk/generated/api/products.service';
import type { ProductSearchSuggestionHttpResponse } from '../../../shared/api/sdk/generated/model/product-search-suggestion-http-response';
import { createProductsSdk } from '../../../shared/api/sdk/products-sdk';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type {
    CreateProductRequest,
    Product,
    ProductFilters,
    ProductOverview,
    ProductSearchSuggestion,
    UpdateProductRequest,
} from '../../../shared/models/product.data';
import { PRODUCT_API_LIMITS } from './product-api.tokens';
import { productFromSdk, productOverviewFromSdk, productPageFromSdk } from './product-sdk.mapper';

export type ProductOverviewQuery = {
    page: number;
    limit: number;
    filters?: ProductFilters;
    includePublic?: boolean;
    recentLimit?: number;
    favoriteLimit?: number;
};

@Service()
export class ProductService {
    private readonly defaultLimits = inject(PRODUCT_API_LIMITS);

    protected readonly baseUrl = environment.apiUrls.products;
    private readonly sdk = createProductsSdk(this.baseUrl, inject(HttpClient));

    public query(page: number, limit: number, filters?: ProductFilters, includePublic = true): Observable<PageOf<Product>> {
        return this.sdk.client
            .queryProducts({ version: this.sdk.version, page, limit, includePublic, ...this.toProductFilters(filters) })
            .pipe(
                map(productPageFromSdk),
                catchError((error: unknown) => rethrowApiError('Query products error', error)),
            );
    }

    public getById(id: string): Observable<Product | null> {
        return this.sdk.client.getProductById({ version: this.sdk.version, id }).pipe(
            map(productFromSdk),
            catchError((error: unknown) => fallbackApiError('Get product error', error, null)),
        );
    }

    public queryOverview(query: ProductOverviewQuery): Observable<ProductOverview> {
        const {
            page,
            limit,
            filters,
            includePublic = true,
            recentLimit = this.defaultLimits.recent,
            favoriteLimit = this.defaultLimits.favorite,
        } = query;
        return this.sdk.client
            .getProductsOverview({
                version: this.sdk.version,
                page,
                limit,
                includePublic,
                recentLimit,
                favoriteLimit,
                ...this.toProductFilters(filters),
            })
            .pipe(
                map(productOverviewFromSdk),
                catchError((error: unknown) => rethrowApiError('Query product overview error', error)),
            );
    }

    private toProductFilters(
        filters?: ProductFilters,
    ): Pick<QueryProductsRequestParams, 'search' | 'productTypes' | 'caloriesFrom' | 'caloriesTo' | 'hasImage'> {
        const { search: rawSearch, productTypes, caloriesFrom, caloriesTo, hasImage } = filters ?? {};
        const search = rawSearch?.trim();
        return {
            search: search === '' ? undefined : search,
            productTypes: productTypes !== undefined && productTypes.length > 0 ? productTypes.join(',') : undefined,
            caloriesFrom,
            caloriesTo,
            hasImage,
        };
    }

    public getRecent(limit?: number, includePublic = true): Observable<Product[]> {
        return this.sdk.client
            .getRecentProducts({ version: this.sdk.version, limit: limit ?? this.defaultLimits.recent, includePublic })
            .pipe(
                map(products => products.map(productFromSdk)),
                catchError((error: unknown) => fallbackApiError('Get recent products error', error, [])),
            );
    }

    public searchSuggestions(search: string, limit?: number): Observable<ProductSearchSuggestion[]> {
        return this.sdk.client
            .getProductSuggestions({ version: this.sdk.version, search, limit: limit ?? this.defaultLimits.suggestions })
            .pipe(
                map(suggestions => suggestions.map(toProductSuggestion)),
                catchError((error: unknown) => fallbackApiError('Search product suggestions error', error, [])),
            );
    }

    public create(data: CreateProductRequest): Observable<Product> {
        return this.sdk.client.createProduct({ version: this.sdk.version, createProductHttpRequest: data }).pipe(
            map(productFromSdk),
            catchError((error: unknown) => rethrowApiError('Create product error', error)),
        );
    }

    public update(id: string, data: UpdateProductRequest): Observable<Product> {
        return this.sdk.client.updateProduct({ version: this.sdk.version, id, updateProductHttpRequest: data }).pipe(
            map(productFromSdk),
            catchError((error: unknown) => rethrowApiError('Update product error', error)),
        );
    }

    public deleteById(id: string): Observable<void> {
        return this.sdk.client.deleteProduct({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Delete product error', error)),
        );
    }

    public duplicate(id: string): Observable<Product> {
        return this.sdk.client.duplicateProduct({ version: this.sdk.version, id }).pipe(
            map(productFromSdk),
            catchError((error: unknown) => rethrowApiError('Duplicate product error', error)),
        );
    }
}

function toProductSuggestion(response: ProductSearchSuggestionHttpResponse): ProductSearchSuggestion {
    const { source, name } = response;
    if (typeof name !== 'string' || (source !== 'openFoodFacts' && source !== 'usda')) {
        throw new Error('Product suggestion does not match the supported application model.');
    }
    return { ...response, source, name };
}
