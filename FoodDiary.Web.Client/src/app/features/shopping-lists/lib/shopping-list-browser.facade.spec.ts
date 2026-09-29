import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingListSummary } from '../models/shopping-list.data';
import { ShoppingListBrowserFacade } from './shopping-list-browser.facade';

function setup(): { service: { getPage: ReturnType<typeof vi.fn> }; facade: ShoppingListBrowserFacade } {
    const service = { getPage: vi.fn() };
    TestBed.configureTestingModule({ providers: [ShoppingListBrowserFacade, { provide: ShoppingListService, useValue: service }] });
    return { service, facade: TestBed.inject(ShoppingListBrowserFacade) };
}
describe('Shopping list lazy loading', () => {
    it('reuses the initial page and requests only the next page', () => {
        const { service, facade } = setup();
        service.getPage.mockReturnValue(of([]));
        facade.seed({
            items: [{ id: 'first', name: 'First', createdAt: '', itemsCount: 0, remainingCount: 0 }],
            hasMore: true,
            nextPage: 2,
        });
        expect(service.getPage).not.toHaveBeenCalled();
        expect(facade.lists()[0].id).toBe('first');
        facade.loadMore();
        expect(service.getPage).toHaveBeenCalledExactlyOnceWith(2, '');
    });
    it('loads one page at a time, deduplicates and stops at the end', () => {
        const { service, facade } = setup();
        const count = 20;
        const rows = Array.from({ length: count }, (_, index) => ({
            id: String(index),
            name: 'List',
            createdAt: '',
            itemsCount: 0,
            remainingCount: 0,
        }));
        const response = new Subject<ShoppingListSummary[]>();
        service.getPage.mockReturnValueOnce(response).mockReturnValueOnce(of([rows[0]]));
        facade.reset('');
        facade.loadMore();
        expect(service.getPage).toHaveBeenCalledTimes(1);
        response.next(rows);
        response.complete();
        facade.loadMore();
        expect(service.getPage).toHaveBeenLastCalledWith(2, '');
        expect(facade.lists()).toHaveLength(count);
        expect(facade.hasMore()).toBe(false);
        facade.loadMore();
        expect(service.getPage).toHaveBeenCalledTimes(2);
    });
    it('cancels stale searches and retries the failed page', () => {
        const { service, facade } = setup();
        const old = new Subject<ShoppingListSummary[]>();
        service.getPage
            .mockReturnValueOnce(old)
            .mockReturnValueOnce(throwError(() => new Error('offline')))
            .mockReturnValueOnce(of([]));
        facade.reset('old');
        facade.cancelSearch();
        facade.reset('new');
        old.next([{ id: 'stale', name: 'Old', createdAt: '', itemsCount: 0 }]);
        expect(facade.lists()).toEqual([]);
        expect(facade.failed()).toBe(true);
        facade.loadMore();
        expect(service.getPage).toHaveBeenLastCalledWith(1, 'new');
        expect(facade.failed()).toBe(false);
    });
});
