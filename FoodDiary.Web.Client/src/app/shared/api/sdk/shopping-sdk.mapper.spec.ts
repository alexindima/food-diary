import { describe, expect, it } from 'vitest';

import type { ShoppingListHttpResponse } from './generated/model/shopping-list-http-response';
import { shoppingListFromSdk, shoppingOverviewFromSdk } from './shopping-sdk.mapper';

const REVISION_LENGTH = 64;
const NON_STRING_REVISION = 42;

function response(): ShoppingListHttpResponse {
    return { id: 'list-1', revision: 'a'.repeat(REVISION_LENGTH), name: 'Groceries', createdAt: '2026-10-22T12:00:00Z', items: [] };
}

describe('shopping list revision decoder', () => {
    it('preserves the exact opaque revision from the detail response', () => {
        expect(shoppingListFromSdk(response()).revision).toBe('a'.repeat(REVISION_LENGTH));
    });

    it.each([
        undefined,
        null,
        '',
        'a'.repeat(REVISION_LENGTH - 1),
        'a'.repeat(REVISION_LENGTH + 1),
        'A'.repeat(REVISION_LENGTH),
        'g'.repeat(REVISION_LENGTH),
        NON_STRING_REVISION,
    ])('rejects a missing or malformed revision: %s', revision => {
        expect(() => shoppingListFromSdk(Object.assign(response(), { revision }))).toThrow();
    });

    it('requires a revision on a selected overview detail but leaves summaries unchanged', () => {
        const summary = { id: 'list-1', name: 'Groceries', createdAt: '2026-10-22T12:00:00Z', itemsCount: 0 };
        const lists = { items: [summary], hasMore: false, nextPage: null };
        expect(shoppingOverviewFromSdk({ selectedList: response(), lists }).selectedList?.revision).toBe('a'.repeat(REVISION_LENGTH));
        expect(shoppingOverviewFromSdk({ selectedList: null, lists }).lists.items).toEqual([summary]);
        expect(() => shoppingOverviewFromSdk({ selectedList: Object.assign(response(), { revision: undefined }), lists })).toThrow();
    });
});
