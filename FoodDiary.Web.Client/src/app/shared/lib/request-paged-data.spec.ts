import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { RequestPagedData } from './request-paged-data';

describe('RequestPagedData', () => {
    it('reports the first page until data arrives and after a failed load clears it', () => {
        const records = new RequestPagedData<string>(signal(false));
        const savedPage = { page: 2, limit: 2, totalPages: 2, totalItems: 3, data: ['Pear'] };

        const initialRequest = records.begin();
        expect(records.isLoading()).toBe(true);
        expect(records.currentPage).toBe(1);
        expect(records.items()).toEqual([]);

        expect(records.succeed(initialRequest, savedPage)).toBe(true);
        expect(records.currentPage).toBe(savedPage.page);
        expect(records.items()).toEqual(savedPage.data);

        const refresh = records.begin();
        expect(records.fail(refresh, 'PRODUCT_LIST.LOAD_ERROR')).toBe(true);
        expect(records.isLoading()).toBe(false);
        expect(records.currentPage).toBe(1);
        expect(records.items()).toEqual([]);
        expect(records.error()).toBe('PRODUCT_LIST.LOAD_ERROR');
    });
});
