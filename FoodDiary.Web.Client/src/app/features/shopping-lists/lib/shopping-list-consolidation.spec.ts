import { describe, expect, it } from 'vitest';

import { entityId, optionalEntityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingListItem } from '../../../shared/models/shopping-list.data';
import { planShoppingConsolidation } from './shopping-list-consolidation';

const ORIGINAL_AMOUNT = 200;

const ITEM: ShoppingListItem = {
    id: entityId<'shopping-list-item'>('a'),
    shoppingListId: entityId<'shopping-list'>('list'),
    name: 'Milk',
    amount: ORIGINAL_AMOUNT,
    unit: 'ML',
    isChecked: false,
    sortOrder: 1,
    sources: [],
};

describe('explicit shopping consolidation', () => {
    it('previews numeric totals without mutating original rows', () => {
        const next = { ...ITEM, id: entityId<'shopping-list-item'>('b'), amount: 300, unit: 'ml' };
        const plan = planShoppingConsolidation([ITEM, next]);
        expect(plan.items).toHaveLength(1);
        expect(plan.items[0]).toMatchObject({ id: 'a', amount: 500 });
        expect(plan.groups[0].before).toEqual([ITEM, next]);
        expect(ITEM.amount).toBe(ORIGINAL_AMOUNT);
    });
    it('collapses matching text quantities', () => {
        const salt = { ...ITEM, name: 'Salt', amount: null, unit: null, note: 'по вкусу' };
        expect(planShoppingConsolidation([salt, { ...salt, id: entityId<'shopping-list-item'>('b') }]).items).toHaveLength(1);
    });
    it.each([
        { unit: 'G' },
        { note: 'Other brand' },
        { category: 'Other' },
        { isChecked: true },
        { productId: entityId<'product'>('different-product') },
        { id: entityId<'shopping-list-item'>('temp-1') },
        { sources: [{ id: 'source', sourceType: 'Recipe', label: 'Recipe', amount: 1 }] },
    ])('preserves incompatible or protected rows %j', override => {
        expect(
            planShoppingConsolidation([
                ITEM,
                {
                    ...ITEM,
                    id: entityId<'shopping-list-item'>('b'),
                    ...override,
                    productId: optionalEntityId<'product'>(override.productId),
                },
            ]).items,
        ).toHaveLength(2);
    });
    it('does not overflow supported amounts', () => {
        expect(
            planShoppingConsolidation([
                { ...ITEM, amount: 1_000_000 },
                { ...ITEM, id: entityId<'shopping-list-item'>('b') },
            ]).items,
        ).toHaveLength(2);
    });
});
