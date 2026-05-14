import { describe, expect, it } from 'vitest';

import { MeasurementUnit } from '../../products/models/product.data';
import type { ShoppingListItem } from '../models/shopping-list.data';
import {
    buildShoppingListItemViewModels,
    buildShoppingListUnitOptions,
    formatShoppingListItemMeta,
    mapShoppingListItemToDto,
    normalizeShoppingListAmount,
    rebuildShoppingListSortOrder,
} from './shopping-list-item.mapper';

const ITEM: ShoppingListItem = {
    id: 'item-1',
    shoppingListId: 'list-1',
    productId: 'product-1',
    name: 'Milk',
    amount: 2,
    unit: MeasurementUnit.ML,
    category: 'Dairy',
    isChecked: false,
    sortOrder: 5,
};
const VALID_DECIMAL_AMOUNT = 1.5;

describe('shopping-list-item.mapper', () => {
    const translate = (key: string): string => (key === 'GENERAL.UNITS.ML' ? 'ml' : key);

    it('should build localized unit options', () => {
        const options = buildShoppingListUnitOptions(translate);

        expect(options).toContainEqual({ value: MeasurementUnit.ML, label: 'ml' });
    });

    it('should format item meta with amount, localized unit and category', () => {
        expect(formatShoppingListItemMeta(ITEM, translate)).toBe('2 ml - Dairy');
    });

    it('should fall back to raw unit when translation is missing', () => {
        const item: ShoppingListItem = { ...ITEM, unit: 'pack' };

        expect(formatShoppingListItemMeta(item, translate)).toBe('2 pack - Dairy');
    });

    it('should build item view models with meta', () => {
        expect(buildShoppingListItemViewModels([ITEM], translate)).toEqual([{ ...ITEM, meta: '2 ml - Dairy' }]);
    });

    it('should rebuild sort order from item positions', () => {
        expect(
            rebuildShoppingListSortOrder([
                { ...ITEM, id: 'a' },
                { ...ITEM, id: 'b' },
            ]).map(item => item.sortOrder),
        ).toEqual([1, 2]);
    });

    it('should normalize non-positive or invalid amounts to null', () => {
        expect(normalizeShoppingListAmount(null)).toBeNull();
        expect(normalizeShoppingListAmount(0)).toBeNull();
        expect(normalizeShoppingListAmount(Number.NaN)).toBeNull();
        expect(normalizeShoppingListAmount(VALID_DECIMAL_AMOUNT)).toBe(VALID_DECIMAL_AMOUNT);
    });

    it('should map item to update dto with rebuilt sort order', () => {
        expect(mapShoppingListItemToDto(ITEM, 0)).toEqual({
            productId: 'product-1',
            name: 'Milk',
            amount: 2,
            unit: MeasurementUnit.ML,
            category: 'Dairy',
            isChecked: false,
            sortOrder: 1,
        });
    });
});
