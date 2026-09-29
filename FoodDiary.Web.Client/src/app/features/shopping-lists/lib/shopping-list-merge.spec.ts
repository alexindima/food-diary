import { describe, expect, it } from 'vitest';

import { appendShoppingItems } from './shopping-list-merge';

const CHICKEN_AMOUNT = 300;

describe('shopping additions merge', () => {
    const chicken = { id: 'existing', productId: 'chicken', name: 'Chicken', amount: 300, unit: 'G', isChecked: false };
    it('sums matching product quantities and preserves the existing identity', () => {
        expect(appendShoppingItems([chicken], [{ ...chicken, id: undefined }])).toEqual([{ ...chicken, amount: 600 }]);
    });
    it('deduplicates matching textual quantities without merging existing duplicates', () => {
        const salt = { name: 'Salt', note: 'to taste' };
        expect(appendShoppingItems([], [salt, salt])).toHaveLength(1);
        expect(
            appendShoppingItems(
                [
                    { ...salt, id: 'a' },
                    { ...salt, id: 'b' },
                ],
                [salt],
            ),
        ).toHaveLength(2);
    });
    it('keeps private or unidentified numeric products, different units, notes and purchased items separate', () => {
        for (const change of [{ productId: null }, { productId: 'other' }, { unit: 'Ml' }, { note: 'fresh' }, { isChecked: true }]) {
            expect(appendShoppingItems([chicken], [{ ...chicken, id: undefined, ...change }])).toHaveLength(2);
        }
        const privateProduct = { name: 'Chicken', amount: 300, unit: 'G' };
        expect(appendShoppingItems([privateProduct], [privateProduct])).toHaveLength(2);
    });
    it('does not mutate inputs or mistake a freeform note for a textual quantity', () => {
        appendShoppingItems([chicken], [chicken]);
        expect(chicken.amount).toBe(CHICKEN_AMOUNT);
        const note = { name: 'Salt', note: 'From the market' };
        expect(appendShoppingItems([], [note, note])).toHaveLength(2);
    });
});
