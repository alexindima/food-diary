import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { describe, expect, it, type Mock,vi } from 'vitest';

import type { GoalHistoryPage } from '../models/user.data';
import { GoalHistoryPager } from './goal-history-pager';

type Item = { id: string; value?: number };
function setup(): {
    pager: GoalHistoryPager<Item>;
    revision: WritableSignal<number>;
    fetchPage: Mock;
    responses: Array<Subject<GoalHistoryPage<Item>>>;
} {
    const responses: Array<Subject<GoalHistoryPage<Item>>> = [];
    const fetchPage = vi.fn(() => {
        const response = new Subject<GoalHistoryPage<Item>>();
        responses.push(response);
        return response;
    });
    const revision = signal(0);
    const pager = TestBed.runInInjectionContext(() => new GoalHistoryPager<Item>(fetchPage, revision));
    TestBed.tick();
    return { pager, revision, fetchPage, responses };
}

describe('GoalHistoryPager', () => {
    it('fetches once on creation and ignores repeated clicks while loading', () => {
        const { pager, fetchPage } = setup();
        pager.loadMore();
        expect(fetchPage).toHaveBeenCalledExactlyOnceWith(undefined);
        expect(pager.loading()).toBe(true);
        expect(pager.loaded()).toBe(false);
    });

    it('appends in server order, deduplicates ids and stops at the last page', () => {
        const { pager, fetchPage, responses } = setup();
        responses[0].next({ items: [{ id: 'a' }, { id: 'b' }], nextCursor: 'next' });
        responses[0].complete();
        pager.loadMore();
        expect(fetchPage).toHaveBeenLastCalledWith('next');
        responses[1].next({ items: [{ id: 'b', value: 1 }, { id: 'c' }], nextCursor: null });
        responses[1].complete();
        expect(pager.items()).toEqual([{ id: 'a' }, { id: 'b', value: 1 }, { id: 'c' }]);
        expect(pager.hasMore()).toBe(false);
        pager.loadMore();
        expect(fetchPage).toHaveBeenCalledTimes(2);
    });

    it('retains rows and retries the same cursor after a failed later page', () => {
        const { pager, fetchPage, responses } = setup();
        responses[0].next({ items: [{ id: 'a' }], nextCursor: 'next' });
        responses[0].complete();
        pager.loadMore();
        responses[1].error(new Error('offline'));
        expect(pager.failed()).toBe(true);
        expect(pager.loading()).toBe(false);
        expect(pager.items()).toEqual([{ id: 'a' }]);
        pager.loadMore();
        expect(fetchPage).toHaveBeenLastCalledWith('next');
        expect(pager.failed()).toBe(false);
        responses[2].next({ items: [{ id: 'b' }], nextCursor: null });
        responses[2].complete();
        expect(pager.items()).toEqual([{ id: 'a' }, { id: 'b' }]);
    });

    it('distinguishes a failed first request from an empty history', () => {
        const { pager, fetchPage, responses } = setup();
        responses[0].error(new Error('offline'));
        expect(pager.loaded()).toBe(false);
        expect(pager.hasMore()).toBe(true);
        pager.loadMore();
        expect(fetchPage).toHaveBeenLastCalledWith(undefined);
        responses[1].next({ items: [], nextCursor: null });
        responses[1].complete();
        expect(pager.loaded()).toBe(true);
        expect(pager.items()).toEqual([]);
        expect(pager.hasMore()).toBe(false);
    });
});

describe('GoalHistoryPager lifecycle', () => {
    it('invalidates pages and cancels stale requests when a goal changes', () => {
        const { pager, revision, fetchPage, responses } = setup();
        responses[0].next({ items: [{ id: 'old' }], nextCursor: 'old-cursor' });
        responses[0].complete();
        pager.loadMore();
        revision.update(value => value + 1);
        TestBed.tick();
        expect(responses[1].observed).toBe(false);
        expect(fetchPage).toHaveBeenLastCalledWith(undefined);
        expect(pager.items()).toEqual([]);
        responses[1].next({ items: [{ id: 'stale' }], nextCursor: null });
        responses[2].next({ items: [{ id: 'fresh' }], nextCursor: null });
        responses[2].complete();
        expect(pager.items()).toEqual([{ id: 'fresh' }]);
    });

    it('cancels an in-flight request when the dialog injector is destroyed', () => {
        const { pager, responses } = setup();
        TestBed.resetTestingModule();
        expect(responses[0].observed).toBe(false);
        expect(pager.loading()).toBe(false);
    });

    it('starts from the first page when a new dialog instance opens', () => {
        const fetchPage = vi.fn(() => of({ items: [{ id: 'a' }], nextCursor: 'next' }));
        const revision = signal(0);
        const first = TestBed.runInInjectionContext(() => new GoalHistoryPager(fetchPage, revision));
        TestBed.tick();
        first.loadMore();
        expect(fetchPage).toHaveBeenLastCalledWith('next');
        TestBed.resetTestingModule();
        TestBed.runInInjectionContext(() => new GoalHistoryPager(fetchPage, revision));
        TestBed.tick();
        expect(fetchPage).toHaveBeenLastCalledWith(undefined);
    });
});
