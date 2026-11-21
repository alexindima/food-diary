import { describe, expect, it } from 'vitest';

import { ProductType } from '../../../../shared/models/product.data';
import { getProductListActiveFilterCount, resolveProductListFilterChanges } from './product-list.state';

const CALORIES_FROM = 100;
const CALORIES_TO = 500;
const ACTIVE_FILTER_COUNT = 5;

describe('product list state', () => {
    it('counts each product type and grouped range/image filters', () => {
        expect(
            getProductListActiveFilterCount({
                onlyMine: true,
                productTypes: [ProductType.Fruit, ProductType.Dairy],
                caloriesFrom: CALORIES_FROM,
                caloriesTo: CALORIES_TO,
                hasImage: false,
            }),
        ).toBe(ACTIVE_FILTER_COUNT);
    });

    it('normalizes duplicate product types and reports exact changes', () => {
        const changes = resolveProductListFilterChanges(
            {
                onlyMine: false,
                productTypes: [ProductType.Fruit],
                caloriesFrom: null,
                caloriesTo: null,
                hasImage: null,
            },
            {
                onlyMine: true,
                productTypes: [ProductType.Dairy, ProductType.Dairy],
                caloriesFrom: CALORIES_FROM,
                caloriesTo: null,
                hasImage: true,
            },
        );

        expect(changes).toEqual({
            productTypes: [ProductType.Dairy],
            onlyMineChanged: true,
            typesChanged: true,
            caloriesChanged: true,
            imageChanged: true,
            hasChanges: true,
        });
    });
});
