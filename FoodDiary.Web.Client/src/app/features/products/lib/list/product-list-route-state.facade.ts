import { inject, Injectable } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map } from 'rxjs';

import { type ProductListQuery, productListQueryKey, readProductListQuery, writeProductListQuery } from './product-list-query';
import type { ProductListQueryState } from './product-list-query-state';

@Injectable()
export class ProductListRouteStateFacade implements ProductListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);

    public readonly initial = readProductListQuery(this.route.snapshot.queryParamMap);
    public readonly changes = this.route.queryParamMap.pipe(
        map(readProductListQuery),
        distinctUntilChanged((left, right) => productListQueryKey(left) === productListQueryKey(right)),
    );

    public async writeAsync(query: ProductListQuery): Promise<boolean> {
        if (productListQueryKey(query) === productListQueryKey(readProductListQuery(this.route.snapshot.queryParamMap))) {
            return false;
        }
        return this.router.navigate([], {
            relativeTo: this.route,
            queryParams: writeProductListQuery(query),
            queryParamsHandling: 'merge',
        });
    }
}
