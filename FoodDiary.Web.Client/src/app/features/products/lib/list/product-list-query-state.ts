import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { ProductListQuery } from './product-list-query';

export type ProductListQueryState = {
    readonly initial: ProductListQuery;
    readonly changes: Observable<ProductListQuery>;
    writeAsync: (query: ProductListQuery) => Promise<boolean>;
};

// Only the page provides this capability. Selection dialogs own local filters.
export const PRODUCT_LIST_QUERY_STATE = new InjectionToken<ProductListQueryState>('ProductListQueryState');
