import { describe, expect, it } from 'vitest';

import { PagedData } from './paged-data.data';

const PAGE = 2;
const LIMIT = 10;
const TOTAL_PAGES = 3;
const TOTAL_ITEMS = 22;

describe('PagedData', () => {
    it('stores page metadata and items', () => {
        const state = new PagedData<string>();

        state.setData({ data: ['a', 'b'], page: PAGE, limit: LIMIT, totalPages: TOTAL_PAGES, totalItems: TOTAL_ITEMS });

        expect(state.items()).toEqual(['a', 'b']);
        expect(state.currentPage).toBe(PAGE);
        expect(state.totalPages).toBe(TOTAL_PAGES);
        expect(state.totalItems).toBe(TOTAL_ITEMS);
    });

    it('clears data to initial state', () => {
        const state = new PagedData<string>();
        state.setData({ data: ['a'], page: PAGE, limit: LIMIT, totalPages: TOTAL_PAGES, totalItems: TOTAL_ITEMS });

        state.clearData();

        expect(state.items()).toEqual([]);
        expect(state.currentPage).toBe(1);
        expect(state.totalPages).toBe(0);
        expect(state.totalItems).toBe(0);
    });

    it('updates loading state', () => {
        const state = new PagedData<string>();

        state.setLoading(true);

        expect(state.isLoading()).toBe(true);
    });
});
