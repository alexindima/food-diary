import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { LessonService } from '../../api/lesson.service';
import type { LessonPage, LessonQuery } from '../../models/lesson.data';
import { LessonFacade } from '../lesson.facade';
import { type LessonListQuery, lessonListQueryKey } from './lesson-list-query';
import { LESSON_LIST_QUERY_STATE } from './lesson-list-query-state';

const initial: LessonListQuery = { page: 2, search: 'iron', category: 'Micronutrients', difficulty: 'Intermediate', sort: 'shortest' };
const current = signal(initial);
const locale = signal('ru');
const changes = new Subject<LessonListQuery>();
const getAll = vi.fn();
const TOTAL_PAGES = 3;
const commitAsync = vi.fn<() => Promise<boolean>>().mockResolvedValue(true);
const writeAsync = vi.fn(async (query: LessonListQuery, _options?: { replaceUrl?: boolean }): Promise<boolean> => {
    if (lessonListQueryKey(query) === lessonListQueryKey(current())) {
        return false;
    }
    current.set(query);
    changes.next(query);
    const committed = await commitAsync();
    return committed;
});

async function settleAsync(): Promise<void> {
    TestBed.tick();
    await waitForAsyncTasksAsync();
    TestBed.tick();
    await waitForAsyncTasksAsync();
}
function response(page = 2, totalPages = TOTAL_PAGES): LessonPage {
    return {
        items: [],
        page,
        pageSize: 18,
        totalCount: 40,
        totalPages,
        totalLessonCount: 40,
        readLessonCount: 0,
        availableCategories: ['Micronutrients'],
    };
}

beforeEach(() => {
    TestBed.resetTestingModule();
    current.set({ ...initial });
    locale.set('ru');
    writeAsync.mockClear();
    getAll.mockReset().mockImplementation((query: LessonQuery) => of(response(query.page)));
    TestBed.configureTestingModule({
        providers: [
            LessonFacade,
            { provide: LessonService, useValue: { getAll, getById: vi.fn(), markRead: vi.fn() } },
            { provide: TranslateService, useValue: { getCurrentLang: (): string => locale(), getFallbackLang: (): string => 'en' } },
            {
                provide: LESSON_LIST_QUERY_STATE,
                useValue: { initial, current, changes, writeAsync, normalizePageAsync: vi.fn().mockResolvedValue(false) },
            },
        ],
    });
});

function cachedPage(): LessonPage {
    return {
        ...response(),
        items: [
            {
                id: 'iron-ru',
                title: 'Железо',
                category: 'Micronutrients',
                difficulty: 'Intermediate',
                estimatedReadMinutes: 9,
                isRead: false,
            },
        ],
    };
}

describe('LessonFacade result identity', () => {
    it('never reuses a previous category while the new category loads or fails', async () => {
        getAll.mockReturnValueOnce(of(cachedPage()));
        const facade = TestBed.inject(LessonFacade);
        await settleAsync();
        expect(facade.lessons()).toHaveLength(1);
        await settleAsync();
        const pending = new Subject<LessonPage>();
        getAll.mockReturnValueOnce(pending);
        const next = { ...initial, category: 'Macronutrients' };
        current.set(next);
        changes.next(next);
        await settleAsync();
        expect(facade.lessons()).toEqual([]);
        pending.error(new Error('Unavailable'));
        await settleAsync();
        expect(facade.hasLoadError()).toBe(true);
        expect(facade.lessons()).toEqual([]);
    });

    it('keeps cached results only for retries with the same locale and query', async () => {
        getAll.mockReturnValueOnce(of(cachedPage()));
        const facade = TestBed.inject(LessonFacade);
        await settleAsync();
        const retry = new Subject<LessonPage>();
        getAll.mockReturnValueOnce(retry);
        facade.retryLessons();
        await settleAsync();
        expect(facade.lessons()).toHaveLength(1);
        const translated = new Subject<LessonPage>();
        getAll.mockReturnValueOnce(translated);
        locale.set('en');
        await settleAsync();
        expect(facade.lessons()).toEqual([]);
        translated.error(new Error('Unavailable'));
        await settleAsync();
        expect(facade.hasLoadError()).toBe(true);
        expect(facade.lessons()).toEqual([]);
    });
});
describe('LessonFacade route list state', () => {
    it('restores all URL fields without default constructor load overwriting them', async () => {
        const facade = TestBed.inject(LessonFacade);
        facade.loadLessons();
        await settleAsync();
        expect(getAll).toHaveBeenLastCalledWith({ ...initial, locale: 'ru', pageSize: 18 });
        expect(facade.pageIndex()).toBe(1);
        expect(facade.categoryFilter()).toBe('Micronutrients');
    });
    it('restores browser query changes and keeps a genuine empty first page without retry loops', async () => {
        const facade = TestBed.inject(LessonFacade);
        await settleAsync();
        getAll.mockImplementation((query: LessonQuery) => of({ ...response(query.page, 0), totalCount: 0 }));
        const query = { ...initial, page: 1, search: 'missing' };
        current.set(query);
        changes.next(query);
        await settleAsync();
        expect(facade.searchQuery()).toBe('missing');
        expect(facade.pageIndex()).toBe(0);
        const calls = getAll.mock.calls.length;
        await settleAsync();
        expect(getAll).toHaveBeenCalledTimes(calls);
    });
    it('recovers only the current out-of-range page with replacement history', async () => {
        getAll.mockImplementation((query: LessonQuery) => of(response(query.page, 1)));
        TestBed.inject(LessonFacade);
        await settleAsync();
        await settleAsync();
        expect(writeAsync).toHaveBeenCalledWith({ ...initial, page: 1 }, { replaceUrl: true });
        expect(getAll).toHaveBeenLastCalledWith({ ...initial, page: 1, locale: 'ru', pageSize: 18 });
    });
    it('ignores an obsolete empty response after a newer browser query', async () => {
        const old = new Subject<LessonPage>();
        getAll.mockReturnValueOnce(old);
        const facade = TestBed.inject(LessonFacade);
        await settleAsync();
        const next = { ...initial, page: 1, category: 'Macronutrients' };
        current.set(next);
        changes.next(next);
        await settleAsync();
        writeAsync.mockClear();
        old.next(response(2, 0));
        old.complete();
        await settleAsync();
        expect(current()).toEqual(next);
        expect(writeAsync).not.toHaveBeenCalledWith(expect.anything(), { replaceUrl: true });
        expect(facade.categoryFilter()).toBe('Macronutrients');
    });
    it('keeps a pending search edit when the old page becomes empty before debounce', async () => {
        const old = new Subject<LessonPage>();
        getAll.mockReturnValueOnce(old);
        const facade = TestBed.inject(LessonFacade);
        await settleAsync();
        facade.searchQuery.set('new draft');
        facade.resetPage();
        TestBed.tick();
        writeAsync.mockClear();
        old.next(response(2, 0));
        old.complete();
        await settleAsync();
        expect(facade.searchQuery()).toBe('new draft');
        expect(writeAsync).not.toHaveBeenCalled();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(current()).toEqual({ ...initial, page: 1, search: 'new draft' });
        });
        await settleAsync();
        expect(getAll).toHaveBeenLastCalledWith({ ...initial, page: 1, search: 'new draft', locale: 'ru', pageSize: 18 });
    });
});
