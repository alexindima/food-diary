import { describe, expect, it } from 'vitest';

import { ProductType } from '../../../../shared/models/product.data';
import { productFilterChips, removeProductFilter, resetProductFilters } from './product-list-filter-chips';
import type { ProductListQuery } from './product-list-query';

const query: ProductListQuery = {
    page: 2,
    search: 'Milk',
    onlyMine: true,
    productTypes: [ProductType.Fruit, ProductType.Dairy],
    caloriesFrom: 0,
    caloriesTo: 500,
    hasImage: false,
};

describe('product filter chips', () => {
    it('shows individual product types, zero and no-photo filters', () => {
        const chips = productFilterChips(query, key => key, 'en');
        expect(chips.map(chip => chip.id)).toEqual([
            'onlyMine',
            `type:${ProductType.Fruit}`,
            `type:${ProductType.Dairy}`,
            'caloriesFrom',
            'caloriesTo',
            'hasImage',
        ]);
        expect(chips.find(chip => chip.id === 'caloriesFrom')?.label).toBe('PRODUCT_LIST.FILTER_CALORIES_FROM: 0');
        expect(chips.find(chip => chip.id === 'hasImage')?.label).toBe('PRODUCT_LIST.FILTER_IMAGE_WITHOUT');
    });

    it('removes one type or bound without discarding the other filters', () => {
        expect(removeProductFilter(query, `type:${ProductType.Fruit}`)).toEqual({ ...query, page: 1, productTypes: [ProductType.Dairy] });
        expect(removeProductFilter(query, 'caloriesFrom')).toEqual({ ...query, page: 1, caloriesFrom: null });
        expect(query.productTypes).toEqual([ProductType.Fruit, ProductType.Dairy]);
        expect(removeProductFilter(query, 'unknown')).toBe(query);
    });

    it('resets structured filters while retaining search and returning to page one', () => {
        const reset = resetProductFilters(query);
        expect(reset).toEqual({
            page: 1,
            search: 'Milk',
            onlyMine: false,
            productTypes: [],
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
        });
        expect(productFilterChips(reset, key => key, 'ru')).toEqual([]);
    });
});
