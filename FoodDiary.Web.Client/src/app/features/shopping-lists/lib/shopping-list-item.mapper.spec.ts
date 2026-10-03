import { describe, expect, it } from 'vitest';

import { MeasurementUnit } from '../../../shared/models/product.data';
import type { ShoppingListItem } from '../../../shared/models/shopping-list.data';
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
    aisle: 'Dairy',
    note: null,
    isChecked: false,
    checkedOnUtc: null,
    sources: [],
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

    it.each(['Ml', 'ml', ' ML '])('localizes persisted unit %s regardless of casing', unit => {
        const translateRu = (key: string): string => (key === 'GENERAL.UNITS.ML' ? 'мл' : key);
        const item = { ...ITEM, amount: 555, unit, category: null };
        expect(buildShoppingListItemViewModels([item], translateRu)[0].quantity).toBe('555 мл');
        expect(buildShoppingListItemViewModels([item], translate)[0].quantity).toBe('555 ml');
    });

    it('should build item view models with meta', () => {
        expect(buildShoppingListItemViewModels([ITEM], translate)).toEqual([
            { ...ITEM, meta: '2 ml - Dairy', quantity: '2 ml', detail: 'Dairy' },
        ]);
    });

    it('puts known text quantities on the right but retains freeform notes as details', () => {
        const [text, note] = buildShoppingListItemViewModels(
            [
                { ...ITEM, amount: null, category: null, note: 'по вкусу' },
                { ...ITEM, amount: null, category: null, note: 'Купить на рынке' },
            ],
            translate,
        );
        expect(text).toMatchObject({ quantity: 'по вкусу', detail: '' });
        expect(note).toMatchObject({ quantity: '', detail: 'Купить на рынке' });
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
            id: 'item-1',
            productId: 'product-1',
            name: 'Milk',
            amount: 2,
            unit: MeasurementUnit.ML,
            category: 'Dairy',
            aisle: 'Dairy',
            note: null,
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 1,
        });
    });
});
