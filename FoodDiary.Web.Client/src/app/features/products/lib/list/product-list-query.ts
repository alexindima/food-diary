import type { ParamMap } from '@angular/router';

import { ProductType } from '../../../../shared/models/product.data';
import { readPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import type { ProductListFilterState } from './product-list.state';

export type ProductListQuery = ProductListFilterState & { search: string | null; page: number };

const productTypes = new Set<string>(Object.values(ProductType));

export function readProductListQuery(params: ParamMap): ProductListQuery {
    const caloriesFrom = readCalories(params.get('caloriesFrom'));
    const caloriesTo = readCalories(params.get('caloriesTo'));
    const hasImage = params.get('hasImage');
    return {
        search: normalizeProductListSearch(params.get('search')),
        page: readPaginationPage(params.get('page')),
        onlyMine: params.get('onlyMine') === 'true',
        productTypes: [...new Set(params.getAll('types').flatMap(value => value.split(',')))].filter(isProductType).sort(),
        caloriesFrom: caloriesFrom !== null && caloriesTo !== null && caloriesFrom > caloriesTo ? null : caloriesFrom,
        caloriesTo,
        hasImage: hasImage === 'true' || hasImage === 'false' ? hasImage === 'true' : null,
    };
}

export function writeProductListQuery(query: ProductListQuery): Record<string, string | null> {
    return {
        search: normalizeProductListSearch(query.search),
        page: query.page === 1 ? null : String(query.page),
        onlyMine: query.onlyMine ? 'true' : null,
        types: query.productTypes.length > 0 ? [...new Set(query.productTypes)].sort().join(',') : null,
        caloriesFrom: query.caloriesFrom === null ? null : String(query.caloriesFrom),
        caloriesTo: query.caloriesTo === null ? null : String(query.caloriesTo),
        hasImage: query.hasImage === null ? null : String(query.hasImage),
    };
}

export function productListQueryKey(query: ProductListQuery): string {
    return JSON.stringify(writeProductListQuery(query));
}

export function normalizeProductListSearch(value: string | null): string | null {
    const trimmed = value?.trim() ?? '';
    return trimmed.length > 0 ? trimmed : null;
}

function isProductType(value: string): value is ProductType {
    return productTypes.has(value);
}

function readCalories(value: string | null): number | null {
    if (value === null || value.trim().length === 0) {
        return null;
    }
    const number = Number(value);
    return Number.isFinite(number) && number >= 0 ? number : null;
}
