import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingListSummary } from '../../../shared/models/shopping-list.data';
import { ShoppingListService } from '../api/shopping-list.service';
import { ShoppingListBrowserFacade } from './shopping-list-browser.facade';

function setup(): { service: { getPage: ReturnType<typeof vi.fn> }; facade: ShoppingListBrowserFacade } {
    const service = { getPage: vi.fn() };
    TestBed.configureTestingModule({ providers: [ShoppingListBrowserFacade, { provide: ShoppingListService, useValue: service }] });
    return { service, facade: TestBed.inject(ShoppingListBrowserFacade) };
}

describe('Shopping list lazy loading', () => {
    it('reuses the initial page and requests only the next page', async () => {
        const { service, facade } = setup();
        service.getPage.mockReturnValue(of([]));
        facade.seed({
            items: [{ id: entityId<'shopping-list'>('first'), name: 'First', createdAt: utcInstant(''), itemsCount: 0, remainingCount: 0 }],
            hasMore: true,
            nextPage: 2,
        });
        await settlePageAsync();
        expect(service.getPage).not.toHaveBeenCalled();
        expect(facade.lists()[0].id).toBe('first');
        facade.loadMore();
        await settlePageAsync();
        expect(service.getPage).toHaveBeenCalledExactlyOnceWith(2, '');
    });
    it('loads one page at a time, deduplicates and stops at the end', async () => {
        const { service, facade } = setup();
        const count = 20;
        const rows = Array.from({ length: count }, (_, index) => ({
            id: entityId<'shopping-list'>(String(index)),
            name: 'List',
            createdAt: utcInstant(''),
            itemsCount: 0,
            remainingCount: 0,
        }));
        const response = new Subject<ShoppingListSummary[]>();
        service.getPage.mockReturnValueOnce(response).mockReturnValueOnce(of([rows[0]]));
        facade.reset('');
        expect(facade.loading()).toBe(true);
        facade.loadMore();
        TestBed.tick();
        facade.loadMore();
        TestBed.tick();
        expect(service.getPage).toHaveBeenCalledExactlyOnceWith(1, '');
        response.next(rows);
        response.complete();
        await settlePageAsync();
        facade.loadMore();
        await settlePageAsync();
        expect(service.getPage).toHaveBeenLastCalledWith(2, '');
        expect(facade.lists()).toHaveLength(count);
        expect(facade.hasMore()).toBe(false);
        facade.loadMore();
        await settlePageAsync();
        expect(service.getPage).toHaveBeenCalledTimes(2);
    });
    it('cancels stale searches and retries the failed page', async () => {
        const { service, facade } = setup();
        const old = new Subject<ShoppingListSummary[]>();
        service.getPage
            .mockReturnValueOnce(old)
            .mockReturnValueOnce(throwError(() => new Error('offline')))
            .mockReturnValueOnce(of([]));
        facade.reset('old');
        TestBed.tick();
        expect(old.observed).toBe(true);
        facade.cancelSearch();
        expect(old.observed).toBe(false);
        facade.reset('new');
        old.next([{ id: entityId<'shopping-list'>('stale'), name: 'Old', createdAt: utcInstant(''), itemsCount: 0 }]);
        await settlePageAsync();
        expect(facade.lists()).toEqual([]);
        expect(facade.failed()).toBe(true);
        facade.loadMore();
        await settlePageAsync();
        expect(service.getPage).toHaveBeenLastCalledWith(1, 'new');
        expect(facade.failed()).toBe(false);
    });
});

describe('Shopping list browser HTTP recovery', () => {
    it.each(['', 'milk'])('preserves a failed page and retries its query "%s"', async search => {
        const { facade, http } = setupWithHttp();
        const first = { id: entityId<'shopping-list'>('first'), name: 'First', createdAt: utcInstant(''), itemsCount: 0, completed: false };
        const second = { id: entityId<'shopping-list'>('second'), name: 'Second', createdAt: utcInstant(''), itemsCount: 0 };
        if (search === '') {
            facade.seed({ items: [first], hasMore: true, nextPage: 2 });
            facade.loadMore();
        } else {
            facade.reset(search);
        }
        const page = search === '' ? 2 : 1;
        const url = pageUrl(page, search);
        TestBed.tick();
        http.expectOne(url).flush('Unavailable', { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await settlePageAsync();
        expect(facade.failed()).toBe(true);
        expect(facade.loading()).toBe(false);
        expect(facade.hasMore()).toBe(true);
        expect(facade.lists()).toEqual(search === '' ? [first] : []);
        facade.loadMore();
        TestBed.tick();
        http.expectOne(url).flush([second]);
        await settlePageAsync();
        expect(facade.failed()).toBe(false);
        expect(facade.loading()).toBe(false);
        expect(facade.hasMore()).toBe(false);
        expect(facade.lists()).toEqual(search === '' ? [first, { ...second, completed: false }] : [{ ...second, completed: false }]);
        http.verify();
    });

    it('aborts the current HTTP request immediately and waits for the replacement search', async () => {
        const { facade, http } = setupWithHttp();
        facade.reset('old');
        TestBed.tick();
        const stale = http.expectOne(pageUrl(1, 'old'));
        expect(stale.cancelled).toBe(false);

        facade.cancelSearch();

        expect(stale.cancelled).toBe(true);
        expect(facade.loading()).toBe(true);
        expect(facade.failed()).toBe(false);
        expect(facade.lists()).toEqual([]);
        facade.loadMore();
        TestBed.tick();
        http.expectNone({ method: 'GET' });

        const result = { id: entityId<'shopping-list'>('new'), name: 'New', createdAt: utcInstant(''), itemsCount: 0 };
        facade.reset('new');
        TestBed.tick();
        http.expectOne(pageUrl(1, 'new')).flush([result]);
        await settlePageAsync();

        expect(facade.lists()).toEqual([{ ...result, completed: false }]);
        expect(facade.loading()).toBe(false);
        http.verify();
    });
});

function setupWithHttp(): { facade: ShoppingListBrowserFacade; http: HttpTestingController } {
    TestBed.configureTestingModule({
        providers: [ShoppingListBrowserFacade, ShoppingListService, provideHttpClient(), provideHttpClientTesting()],
    });
    return { facade: TestBed.inject(ShoppingListBrowserFacade), http: TestBed.inject(HttpTestingController) };
}

function pageUrl(page: number, search: string): string {
    return `${environment.apiUrls.shoppingLists}/page?page=${page}&limit=20&search=${search}`;
}

async function settlePageAsync(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
}
