import { describe, expect, it } from 'vitest';

import { buildShoppingListOptions } from './shopping-list-options.mapper';

describe('buildShoppingListOptions', () => {
    it('should map list summaries to select options with item counts', () => {
        expect(
            buildShoppingListOptions([
                { id: 'list-1', name: 'Groceries', createdAt: '2026-01-01T00:00:00Z', itemsCount: 3 },
                { id: 'list-2', name: 'Weekend', createdAt: '2026-01-02T00:00:00Z', itemsCount: 0 },
            ]),
        ).toEqual([
            { value: 'list-1', label: 'Groceries (3)' },
            { value: 'list-2', label: 'Weekend (0)' },
        ]);
    });
});
