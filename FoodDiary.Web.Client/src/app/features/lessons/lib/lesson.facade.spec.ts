import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { EMPTY, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { LessonService } from '../api/lesson.service';
import type { LessonDetail, LessonPage, LessonSummary } from '../models/lesson.data';
import { LessonFacade } from './lesson.facade';
import { LessonListFacade } from './list/lesson-list.facade';
import { type LessonListQuery, lessonListQueryKey } from './list/lesson-list-query';
import { LESSON_LIST_QUERY_STATE, type LessonListQueryState } from './list/lesson-list-query-state';

const WAIT_ATTEMPTS = 20;
const READ_MINUTES = 5;
const OUT_OF_RANGE_PAGE = 99;

type LessonServiceMock = {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    markRead: ReturnType<typeof vi.fn>;
};

let facade: LessonFacade;
let listFacade: LessonListFacade;
let lessonService: LessonServiceMock;
let translateService: {
    getCurrentLang: ReturnType<typeof vi.fn>;
    getFallbackLang: ReturnType<typeof vi.fn>;
};

describe('Lesson list and detail request lifetime', () => {
    beforeEach(setupFacade);

    it('unsubscribes catalogue and detail reads on owner destruction', async () => {
        TestBed.inject(LessonListFacade);
        const page = new Subject<LessonPage>();
        const detail = new Subject<LessonDetail>();
        lessonService.getAll.mockReturnValue(page);
        lessonService.getById.mockReturnValue(detail);
        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => page.observed && detail.observed);
        expect([page.observed, detail.observed]).toEqual([true, true]);

        TestBed.resetTestingModule();

        expect([page.observed, detail.observed]).toEqual([false, false]);
    });

    it('unsubscribes old catalogue and detail requests when their parameters change', async () => {
        const catalogue = TestBed.inject(LessonListFacade);
        const page = new Subject<LessonPage>();
        const detail = new Subject<LessonDetail>();
        lessonService.getAll.mockReturnValueOnce(page);
        lessonService.getById
            .mockReturnValueOnce(detail)
            .mockReturnValueOnce(of({ ...createDetail(), id: entityId<'lesson'>('lesson-2') }));
        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => page.observed && detail.observed);
        catalogue.categoryFilter.set('Micronutrients');
        facade.loadLesson(entityId<'lesson'>('lesson-2'));
        await waitForAsync(() => facade.selectedLesson()?.id === 'lesson-2' && !catalogue.isLoading());

        expect([page.observed, detail.observed]).toEqual([false, false]);
        expect(facade.selectedLesson()?.id).toBe('lesson-2');
    });

    it('does not normalize a late catalogue response after its owner is destroyed', async () => {
        TestBed.resetTestingModule();
        const query: LessonListQuery = { page: OUT_OF_RANGE_PAGE, search: '', category: null, difficulty: null, sort: 'recommended' };
        const pending = new Subject<LessonPage>();
        lessonService.getAll.mockReturnValue(pending);
        const writeAsync = vi.fn().mockResolvedValue(true);
        TestBed.configureTestingModule({
            providers: [
                LessonListFacade,
                { provide: LessonService, useValue: lessonService },
                { provide: TranslateService, useValue: translateService },
                {
                    provide: LESSON_LIST_QUERY_STATE,
                    useValue: { ...createCatalogueRoute(query), writeAsync },
                },
            ],
        });
        TestBed.inject(LessonListFacade);
        await waitForAsync(() => pending.observed);
        expect(pending.observed).toBe(true);
        writeAsync.mockClear();
        TestBed.resetTestingModule();
        pending.next(createPage());
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(writeAsync).not.toHaveBeenCalled();
    });

    it('cancels an old read-progress action and leaves the new lesson usable', async () => {
        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => facade.selectedLesson() !== null);
        const pending = new Subject<void>();
        lessonService.markRead.mockReturnValueOnce(pending);
        facade.markRead(entityId<'lesson'>('lesson-1'));
        lessonService.getById.mockReturnValue(of({ ...createDetail(), id: entityId<'lesson'>('lesson-2') }));
        facade.loadLesson(entityId<'lesson'>('lesson-2'));
        await waitForAsync(() => facade.selectedLesson()?.id === 'lesson-2');

        expect(pending.observed).toBe(false);
        expect(facade.isMarkingRead()).toBe(false);
        pending.error(new Error('Late old request'));
        expect(facade.markReadFailed()).toBe(false);
        facade.markRead(entityId<'lesson'>('lesson-2'));
        expect(facade.selectedLesson()?.isRead).toBe(true);
    });
});

describe('LessonFacade', () => {
    beforeEach(setupFacade);

    it('distinguishes a missing lesson from a temporary loading failure', async () => {
        lessonService.getById.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));
        facade.loadLesson(entityId<'lesson'>('missing'));
        await waitForAsync(() => facade.hasDetailError());
        expect(facade.isLessonMissing()).toBe(true);
        lessonService.getById.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable })));
        facade.retryLesson();
        await waitForAsync(() => facade.hasDetailError() && !facade.isLessonMissing());
        expect(facade.isLessonMissing()).toBe(false);
        expect(facade.hasDetailError()).toBe(true);
    });

    it('exposes detail loading failure and recovers the selected lesson', async () => {
        lessonService.getById.mockReturnValue(throwError(() => new Error('Unavailable')));
        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => facade.hasDetailError());
        expect(facade.hasDetailError()).toBe(true);
        expect(facade.selectedLesson()).toBeNull();
        lessonService.getById.mockReturnValue(of(createDetail()));
        facade.retryLesson();
        await waitForAsync(() => facade.selectedLesson() !== null);
        expect(facade.hasDetailError()).toBe(false);
        expect(facade.selectedLesson()?.id).toBe('lesson-1');
    });

    it('prevents duplicate read requests and retains unread state on failure until retry succeeds', async () => {
        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => facade.selectedLesson() !== null);
        const request = new Subject<void>();
        lessonService.markRead.mockReturnValue(request);
        facade.markRead(entityId<'lesson'>('lesson-1'));
        facade.markRead(entityId<'lesson'>('lesson-1'));
        expect(lessonService.markRead).toHaveBeenCalledTimes(1);
        expect(facade.isMarkingRead()).toBe(true);
        request.error(new Error('Unavailable'));
        expect(facade.isMarkingRead()).toBe(false);
        expect(facade.markReadFailed()).toBe(true);
        expect(facade.selectedLesson()?.isRead).toBe(false);

        lessonService.markRead.mockReturnValue(of(undefined));
        facade.markRead(entityId<'lesson'>('lesson-1'));
        expect(facade.markReadFailed()).toBe(false);
        expect(facade.selectedLesson()?.isRead).toBe(true);
        expect(facade.isMarkingRead()).toBe(false);
    });
});

describe('LessonListFacade catalogue', () => {
    beforeEach(setupListFacade);

    it('retries failed catalogue loading with the selected filters intact', async () => {
        lessonService.getAll.mockReturnValue(throwError(() => new Error('Unavailable')));
        listFacade.categoryFilter.set('Macronutrients');
        listFacade.difficultyFilter.set('Beginner');
        listFacade.sortOrder.set('shortest');
        await waitForAsync(() => listFacade.hasLoadError());
        expect(listFacade.hasLoadError()).toBe(true);

        lessonService.getAll.mockReturnValue(of(createPage()));
        listFacade.retryLessons();
        await waitForAsync(() => !listFacade.hasLoadError() && listFacade.lessons().length > 0);
        expect(listFacade.hasLoadError()).toBe(false);
        expect(lessonService.getAll).toHaveBeenLastCalledWith(
            expect.objectContaining({
                category: 'Macronutrients',
                difficulty: 'Beginner',
                sort: 'shortest',
            }),
        );
    });

    it('loads lessons with normalized current locale and category filter', async () => {
        listFacade.categoryFilter.set('Macronutrients');

        await waitForAsync(() => listFacade.lessons().length > 0);

        expect(lessonService.getAll).toHaveBeenLastCalledWith({
            locale: 'ru',
            category: 'Macronutrients',
            difficulty: undefined,
            search: undefined,
            sort: 'recommended',
            page: 1,
            pageSize: 18,
        });
        expect(listFacade.lessons()).toEqual([createSummary()]);
    });

    it('falls back to English locale when translate service has no language', async () => {
        translateService.getCurrentLang.mockReturnValue('');
        translateService.getFallbackLang.mockReturnValue('');
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                LessonListFacade,
                { provide: LessonService, useValue: lessonService },
                { provide: TranslateService, useValue: translateService },
                { provide: LESSON_LIST_QUERY_STATE, useValue: createCatalogueRoute() },
            ],
        });
        listFacade = TestBed.inject(LessonListFacade);

        await waitForAsync(() => lessonService.getAll.mock.calls.length > 0);

        expect(lessonService.getAll).toHaveBeenCalledWith({
            locale: 'en',
            category: undefined,
            difficulty: undefined,
            search: undefined,
            sort: 'recommended',
            page: 1,
            pageSize: 18,
        });
    });
});

describe('LessonFacade detail and reading progress', () => {
    beforeEach(setupFacade);

    it('loads selected lesson detail and returns null for empty selection', async () => {
        expect(facade.selectedLesson()).toBeNull();

        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => facade.selectedLesson() !== null);

        expect(lessonService.getById).toHaveBeenCalledWith('lesson-1');
        expect(facade.selectedLesson()).toEqual(createDetail());

        facade.loadLesson(null);
        await waitForAsync(() => !facade.isDetailLoading());
        expect(facade.selectedLesson()).toBeNull();
        expect(lessonService.getById).toHaveBeenCalledExactlyOnceWith('lesson-1');
    });

    it('marks the selected lesson as read after service succeeds', async () => {
        facade.loadLesson(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => facade.selectedLesson() !== null);

        facade.markRead(entityId<'lesson'>('lesson-1'));
        await waitForAsync(() => facade.selectedLesson()?.isRead === true);

        expect(lessonService.markRead).toHaveBeenCalledWith('lesson-1');
        expect(facade.selectedLesson()?.isRead).toBe(true);
    });
});

describe('LessonListFacade refresh state', () => {
    beforeEach(setupListFacade);

    it('does not show the previous category while a new category loads', async () => {
        await waitForAsync(() => listFacade.lessons().length > 0);
        const nextPage = new Subject<LessonPage>();
        lessonService.getAll.mockReturnValue(nextPage);

        listFacade.categoryFilter.set('Micronutrients');
        await waitForAsync(() => listFacade.isLoading());

        expect(listFacade.lessons()).toEqual([]);
        expect(listFacade.page().availableCategories).toEqual([]);

        nextPage.next({
            ...createPage(),
            items: [{ ...createSummary(), id: entityId<'lesson'>('lesson-2'), category: 'Micronutrients' }],
            availableCategories: ['Macronutrients', 'Micronutrients'],
        });
        nextPage.complete();
        await waitForAsync(() => listFacade.lessons().at(0)?.id === 'lesson-2');

        expect(listFacade.page().availableCategories).toEqual(['Macronutrients', 'Micronutrients']);
    });
});

function setupFacade(): void {
    setupLessonProviders();
    facade = TestBed.inject(LessonFacade);
}

function setupListFacade(): void {
    setupLessonProviders();
    listFacade = TestBed.inject(LessonListFacade);
}

function setupLessonProviders(): void {
    TestBed.resetTestingModule();
    lessonService = {
        getAll: vi.fn(() => of(createPage())),
        getById: vi.fn(() => of(createDetail())),
        markRead: vi.fn(() => of(void 0)),
    };
    translateService = {
        getCurrentLang: vi.fn(() => 'ru-RU'),
        getFallbackLang: vi.fn(() => 'en'),
    };

    TestBed.configureTestingModule({
        providers: [
            LessonFacade,
            LessonListFacade,
            { provide: LessonService, useValue: lessonService },
            { provide: TranslateService, useValue: translateService },
            { provide: LESSON_LIST_QUERY_STATE, useValue: createCatalogueRoute() },
        ],
    });
}

function createCatalogueRoute(bookmark?: LessonListQuery): LessonListQueryState {
    const initial: LessonListQuery = bookmark ?? { page: 1, search: '', category: null, difficulty: null, sort: 'recommended' };
    const current = signal(initial, { equal: (left, right) => lessonListQueryKey(left) === lessonListQueryKey(right) });
    const commitAsync = vi.fn<() => Promise<boolean>>().mockResolvedValue(true);
    return {
        initial,
        current,
        changes: EMPTY,
        isCurrent: query => lessonListQueryKey(query) === lessonListQueryKey(current()),
        writeAsync: async (query): Promise<boolean> => {
            current.set(query);
            return commitAsync();
        },
        normalizePageAsync: vi.fn().mockResolvedValue(false),
    };
}

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        TestBed.tick();

        if (predicate()) {
            return;
        }

        await waitForAsyncTasksAsync();
    }
}

function createSummary(): LessonSummary {
    return {
        id: entityId<'lesson'>('lesson-1'),
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: READ_MINUTES,
        isRead: false,
    };
}

function createPage(): LessonPage {
    return {
        items: [createSummary()],
        page: 1,
        pageSize: 18,
        totalCount: 1,
        totalPages: 1,
        totalLessonCount: 1,
        readLessonCount: 0,
        availableCategories: ['Macronutrients'],
    };
}

function createDetail(): LessonDetail {
    return {
        ...createSummary(),
        content: 'Lesson content',
    };
}
