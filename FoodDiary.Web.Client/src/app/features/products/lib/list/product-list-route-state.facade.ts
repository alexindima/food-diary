import { computed, inject, Injectable, type Signal, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { distinctUntilChanged } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type ProductListQuery, productListQueryKey, readProductListQuery, writeProductListQuery } from './product-list-query';
import type { ProductListQueryState } from './product-list-query-state';

@Injectable()
export class ProductListRouteStateFacade implements ProductListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly boundQuery = signal<Signal<ProductListQuery> | null>(null);

    public readonly initial = readProductListQuery(this.committedParams());
    public readonly changes = toObservable(computed(() => this.boundQuery()?.() ?? this.initial)).pipe(
        distinctUntilChanged((left, right) => productListQueryKey(left) === productListQueryKey(right)),
    );

    public bind(query: Signal<ProductListQuery>): void {
        this.boundQuery.set(query);
    }

    public matchesCommittedQuery(query: ProductListQuery): boolean {
        return productListQueryKey(query) === productListQueryKey(readProductListQuery(this.committedParams()));
    }

    public async normalizePageAsync(query: ProductListQuery): Promise<boolean> {
        return hasInvalidPaginationPage(this.committedParams().get('page')) && this.writeAsync(query, { replaceUrl: true });
    }

    public async writeAsync(query: ProductListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        if (this.matchesCommittedQuery(query) && !hasInvalidPaginationPage(this.committedParams().get('page'))) {
            return false;
        }
        return this.router.navigate([], {
            relativeTo: this.route,
            queryParams: writeProductListQuery(query),
            queryParamsHandling: 'merge',
            replaceUrl: options?.replaceUrl ?? false,
        });
    }

    private committedParams(): ReturnType<typeof convertToParamMap> {
        return convertToParamMap(this.route.snapshot.queryParams);
    }
}
