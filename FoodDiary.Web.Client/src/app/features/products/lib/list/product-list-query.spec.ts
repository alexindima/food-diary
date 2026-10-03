import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { ProductType } from '../../../../shared/models/product.data';
import { productListQueryKey, readProductListQuery, writeProductListQuery } from './product-list-query';

const UPPER_CALORIE_BOUND = 100;

describe('Product list URL query', () => {
    it('round-trips search, filters and pagination with canonical types', () => {
        const query = readProductListQuery(
            convertToParamMap({
                search: ' tea ',
                onlyMine: 'true',
                types: ['Meat,Fruit', 'Fruit'],
                caloriesFrom: '10',
                caloriesTo: '200',
                hasImage: 'false',
                page: '3',
            }),
        );
        expect(query).toEqual({
            search: 'tea',
            onlyMine: true,
            productTypes: [ProductType.Fruit, ProductType.Meat],
            caloriesFrom: 10,
            caloriesTo: 200,
            hasImage: false,
            page: 3,
        });
        expect(readProductListQuery(convertToParamMap(writeProductListQuery(query)))).toEqual(query);
    });

    it('ignores malformed query values before making an API request', () => {
        expect(
            readProductListQuery(
                convertToParamMap({
                    page: 'Infinity',
                    types: 'Invalid,Fruit',
                    caloriesFrom: '-1',
                    caloriesTo: 'NaN',
                    hasImage: 'maybe',
                    onlyMine: 'yes',
                    search: ' ',
                }),
            ),
        ).toEqual({
            search: null,
            onlyMine: false,
            productTypes: [ProductType.Fruit],
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
            page: 1,
        });
    });

    it('removes defaults and normalizes equivalent URLs', () => {
        const query = readProductListQuery(convertToParamMap({}));
        expect(Object.values(writeProductListQuery(query))).toEqual(Array.from({ length: 7 }).fill(null));
        expect(productListQueryKey(query)).toBe(productListQueryKey(readProductListQuery(convertToParamMap({ page: '1', search: ' ' }))));
    });

    it('drops an inverted lower calorie bound', () => {
        const query = readProductListQuery(convertToParamMap({ caloriesFrom: '200', caloriesTo: '100' }));
        expect(query.caloriesFrom).toBeNull();
        expect(query.caloriesTo).toBe(UPPER_CALORIE_BOUND);
    });
});
