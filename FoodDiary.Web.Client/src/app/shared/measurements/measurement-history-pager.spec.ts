import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { MeasurementHistoryPager, previousMeasurementDay } from './measurement-history-pager';

type Entry = { id: string; date: string; value: number };
const PAGE_SIZE = 20;
const CALENDAR_DATE_LENGTH = 10;
const FETCH_SIZE = PAGE_SIZE + 1;
const LARGE_HISTORY_SIZE = 523;
const LAST_PAGE_SIZE = 3;
function entries(count: number, offset = 0): Entry[] {
    return Array.from({ length: count }, (_, i) => {
        const date = new Date('2026-09-21T00:00:00Z');
        date.setUTCDate(date.getUTCDate() - offset - i);
        return { id: String(offset + i), date: date.toISOString(), value: offset + i };
    });
}
function setup(): { pager: MeasurementHistoryPager<Entry>; fetch: ReturnType<typeof vi.fn>; responses: Array<Subject<Entry[]>> } {
    const responses: Array<Subject<Entry[]>> = [];
    const fetch = vi.fn(() => {
        const response = new Subject<Entry[]>();
        responses.push(response);
        return response;
    });
    const pager = TestBed.runInInjectionContext(() => new MeasurementHistoryPager(fetch));
    return { pager, fetch, responses };
}
function respond(response: Subject<Entry[]>, values: Entry[]): void {
    response.next(values);
    response.complete();
}
describe('MeasurementHistoryPager', () => {
    it('loads on dialog creation and guards overlapping clicks', () => {
        const { pager, fetch } = setup();
        pager.loadMore();
        expect(fetch).toHaveBeenCalledExactlyOnceWith(undefined);
        expect(pager.loading()).toBe(true);
        expect(pager.visibleCount()).toBe(0);
        expect(pager.message()).toBeNull();
    });
    it('hides lookahead but preserves its value for delta and refetches it on the next page', () => {
        const { pager, fetch, responses } = setup();
        const first = entries(FETCH_SIZE);
        respond(responses[0], first);
        expect(pager.visibleCount()).toBe(PAGE_SIZE);
        expect(pager.entries().at(-1)).toEqual(first.at(-1));
        pager.loadMore();
        expect(fetch).toHaveBeenLastCalledWith(previousMeasurementDay(first[PAGE_SIZE - 1].date));
        const final = entries(LAST_PAGE_SIZE, PAGE_SIZE);
        final[0].value = 99;
        respond(responses[1], final);
        expect(pager.entries()).toEqual([...first.slice(0, PAGE_SIZE), ...final]);
        expect(pager.visibleCount()).toBe(PAGE_SIZE + LAST_PAGE_SIZE);
        expect(pager.hasMore()).toBe(false);
        pager.loadMore();
        expect(fetch).toHaveBeenCalledTimes(2);
    });
    it('handles deletion of the hidden lookahead without retaining a phantom record', () => {
        const { pager, responses } = setup();
        respond(responses[0], entries(FETCH_SIZE));
        pager.loadMore();
        respond(responses[1], entries(2, FETCH_SIZE));
        expect(pager.entries().some(entry => entry.id === String(PAGE_SIZE))).toBe(false);
        expect(pager.visibleCount()).toBe(PAGE_SIZE + 2);
    });
    it.each([0, 1, PAGE_SIZE])('stops immediately for a terminal page of %s rows', count => {
        const { pager, fetch, responses } = setup();
        respond(responses[0], entries(count));
        expect(pager.visibleCount()).toBe(count);
        expect(pager.hasMore()).toBe(false);
        pager.loadMore();
        expect(fetch).toHaveBeenCalledTimes(1);
        expect(pager.message()).toBe(count === 0 ? 'MEASUREMENT_HISTORY_PAGING.EMPTY' : null);
    });
});

describe('Measurement history errors and lifecycle', () => {
    it('keeps loaded rows and retries the same date after errors', () => {
        const { pager, fetch, responses } = setup();
        const first = entries(FETCH_SIZE);
        respond(responses[0], first);
        pager.loadMore();
        responses[1].error(new Error('offline'));
        expect(pager.entries()).toEqual(first);
        expect(pager.visibleCount()).toBe(PAGE_SIZE);
        expect(pager.failed()).toBe(true);
        expect(pager.loading()).toBe(false);
        pager.loadMore();
        expect(fetch.mock.calls[2]).toEqual(fetch.mock.calls[1]);
        expect(pager.failed()).toBe(false);
        respond(responses[2], []);
        expect(pager.visibleCount()).toBe(PAGE_SIZE);
        expect(pager.hasMore()).toBe(false);
    });
    it('distinguishes initial failure from empty history and supports retry', () => {
        const { pager, responses } = setup();
        responses[0].error(new Error('offline'));
        expect(pager.loaded()).toBe(false);
        expect(pager.message()).toBe('MEASUREMENT_HISTORY_PAGING.ERROR');
        pager.loadMore();
        respond(responses[1], []);
        expect(pager.message()).toBe('MEASUREMENT_HISTORY_PAGING.EMPTY');
    });
    it('cancels HTTP work on close and ignores late responses', () => {
        const { pager, responses } = setup();
        TestBed.resetTestingModule();
        expect(responses[0].observed).toBe(false);
        responses[0].next(entries(1));
        expect(pager.entries()).toEqual([]);
    });
    it('a reopened dialog starts fresh without sharing cached rows or cursors', () => {
        const first = setup();
        respond(first.responses[0], entries(FETCH_SIZE));
        const second = setup();
        expect(second.fetch).toHaveBeenCalledExactlyOnceWith(undefined);
        expect(second.pager.entries()).toEqual([]);
    });
    it('reaches beyond 500 entries without duplicates or skipped dates when newer rows change', () => {
        let database = entries(LARGE_HISTORY_SIZE);
        const fetch = vi.fn((dateTo?: string) =>
            of(database.filter(entry => dateTo === undefined || entry.date.slice(0, CALENDAR_DATE_LENGTH) <= dateTo).slice(0, FETCH_SIZE)),
        );
        const pager = TestBed.runInInjectionContext(() => new MeasurementHistoryPager(fetch));
        database = [{ id: 'new', date: '2026-09-22', value: 0 }, ...database.slice(1)];
        while (pager.hasMore()) {
            pager.loadMore();
        }
        expect(pager.entries()).toEqual(entries(LARGE_HISTORY_SIZE));
        expect(new Set(pager.entries().map(entry => entry.id)).size).toBe(LARGE_HISTORY_SIZE);
    });
});
describe('Measurement date cursor', () => {
    it.each([
        ['2026-01-01', '2025-12-31'],
        ['2024-03-01T00:00:00Z', '2024-02-29'],
        ['2026-03-01', '2026-02-28'],
        ['2026-03-09T00:00:00Z', '2026-03-08'],
        ['2026-11-02T00:00:00Z', '2026-11-01'],
        ['2026-09-21T00:00:00+04:00', '2026-09-20'],
    ])('moves %s back one calendar day, independent of machine timezone', (value, expected) => {
        expect(previousMeasurementDay(value)).toBe(expected);
    });
});
