import { describe, expect, it } from 'vitest';

import { MeasurementUnit, ProductType } from '../../models/product.data';
import { buildFavoriteProductSnapshot, getProductListActiveFilterCount, resolveProductListFilterChanges } from './product-list.state';

const CALORIES_FROM = 100;
const CALORIES_TO = 500;
const DEFAULT_PORTION = 150;
const ACTIVE_FILTER_COUNT = 5;
const FAVORITE_GRAM_BASE_AMOUNT = 100;

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

    it.each([
        ['ML', MeasurementUnit.ML, FAVORITE_GRAM_BASE_AMOUNT],
        ['PCS', MeasurementUnit.PCS, 1],
        ['UNKNOWN', MeasurementUnit.G, FAVORITE_GRAM_BASE_AMOUNT],
    ] as const)('maps favorite unit %s to snapshot unit %s', (sourceUnit, expectedUnit, expectedAmount) => {
        const snapshot = buildFavoriteProductSnapshot(createFavorite(sourceUnit));

        expect(snapshot.name).toBe('Fallback name');
        expect(snapshot.baseUnit).toBe(expectedUnit);
        expect(snapshot.baseAmount).toBe(expectedAmount);
        expect(snapshot.defaultPortionAmount).toBe(DEFAULT_PORTION);
        expect(snapshot.isFavorite).toBe(true);
    });
});

function createFavorite(baseUnit: string): Parameters<typeof buildFavoriteProductSnapshot>[0] {
    return {
        id: 'favorite-1',
        productId: 'product-1',
        productName: 'Fallback name',
        name: '   ',
        barcode: null,
        brand: null,
        comment: null,
        imageUrl: null,
        baseUnit,
        defaultPortionAmount: DEFAULT_PORTION,
        preferredPortionAmount: DEFAULT_PORTION,
        caloriesPerBase: 1,
        proteinsPerBase: 2,
        fatsPerBase: 3,
        carbsPerBase: 4,
        fiberPerBase: 5,
        alcoholPerBase: 0,
        createdAtUtc: '2026-04-12T10:00:00Z',
        isOwnedByCurrentUser: true,
        qualityScore: 0,
        qualityGrade: 'red',
    };
}
