import { describe, expect, it } from 'vitest';

import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { buildShoppingListOptions } from './shopping-list-options.mapper';

describe('buildShoppingListOptions', () => {
    it('should map list summaries to select options with item counts', () => {
        expect(
            buildShoppingListOptions([
                {
                    id: entityId<'shopping-list'>('list-1'),
                    name: 'Groceries',
                    createdAt: utcInstant('2026-01-01T00:00:00Z'),
                    itemsCount: 3,
                },
                { id: entityId<'shopping-list'>('list-2'), name: 'Weekend', createdAt: utcInstant('2026-01-02T00:00:00Z'), itemsCount: 0 },
            ]),
        ).toEqual([
            { value: 'list-1', label: 'Groceries (3)' },
            { value: 'list-2', label: 'Weekend (0)' },
        ]);
    });
});
