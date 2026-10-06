import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { adminPageWithinTotal, restoreAdminPage } from './admin-pagination';

const PAGE_SIZE = 20;
const TWO_PAGE_TOTAL = 21;
const SMALL_TOTAL = 30;
const GROUP_SIZE = 50;
const FIFTH_PAGE = 5;
const STALE_PAGE = 99;
const OVERSIZED_PAGE = 999999;
const MAX_PAGE = 10000;
const OVER_MAX_PAGE = 10001;
const LARGE_TOTAL = 1000000;

describe('admin pagination recovery', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideRouter([])] });
    });

    it.each([
        [1, 0, PAGE_SIZE, 1],
        [FIFTH_PAGE, TWO_PAGE_TOTAL, PAGE_SIZE, 2],
        [OVERSIZED_PAGE, SMALL_TOTAL, PAGE_SIZE, 2],
        [Number.NaN, SMALL_TOTAL, PAGE_SIZE, 1],
        [2, GROUP_SIZE, GROUP_SIZE, 1],
        [OVER_MAX_PAGE, LARGE_TOTAL, PAGE_SIZE, MAX_PAGE],
    ])('bounds page %s against %s records with page size %s', (page, total, size, expected) => {
        expect(adminPageWithinTotal(page, total, size)).toBe(expected);
    });

    it('recovers a stale bookmark without discarding filters or adding a history entry', async () => {
        const router = TestBed.inject(Router);
        const route = TestBed.inject(ActivatedRoute);
        await router.navigateByUrl('/?page=99&search=kept&status=inactive');
        const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
        expect(restoreAdminPage(router, route, STALE_PAGE, { totalItems: TWO_PAGE_TOTAL, pageSize: PAGE_SIZE })).toBe(true);
        expect(navigate).toHaveBeenCalledWith([], {
            relativeTo: route,
            queryParams: { page: 2 },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    });

    it('does not refetch a valid page or an empty default list', async () => {
        const router = TestBed.inject(Router);
        const route = TestBed.inject(ActivatedRoute);
        const navigate = vi.spyOn(router, 'navigate');
        expect(restoreAdminPage(router, route, 1, { totalItems: 0, pageSize: PAGE_SIZE })).toBe(false);
        expect(navigate).not.toHaveBeenCalled();
        await router.navigateByUrl('/?page=2');
        navigate.mockClear();
        expect(restoreAdminPage(router, route, 2, { totalItems: TWO_PAGE_TOTAL, pageSize: PAGE_SIZE })).toBe(false);
        expect(navigate).not.toHaveBeenCalled();
    });
});
