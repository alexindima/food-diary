import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { LessonService } from '../api/lesson.service';
import type { LessonDetail, LessonPage, LessonSummary } from '../models/lesson.data';
import { LessonFacade } from './lesson.facade';

const WAIT_ATTEMPTS = 20;
const READ_MINUTES = 5;

type LessonServiceMock = {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    markRead: ReturnType<typeof vi.fn>;
};

let facade: LessonFacade;
let lessonService: LessonServiceMock;
let translateService: {
    getCurrentLang: ReturnType<typeof vi.fn>;
    getFallbackLang: ReturnType<typeof vi.fn>;
};

describe('LessonFacade', () => {
    beforeEach(setupFacade);

    it('loads lessons with normalized current locale and category filter', async () => {
        facade.loadLessons('Macronutrients');

        await waitForAsync(() => facade.lessons().length > 0);

        expect(lessonService.getAll).toHaveBeenLastCalledWith({
            locale: 'ru',
            category: 'Macronutrients',
            difficulty: undefined,
            search: undefined,
            sort: 'recommended',
            page: 1,
            pageSize: 18,
        });
        expect(facade.lessons()).toEqual([createSummary()]);
    });

    it('falls back to English locale when translate service has no language', async () => {
        translateService.getCurrentLang.mockReturnValue('');
        translateService.getFallbackLang.mockReturnValue('');
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                LessonFacade,
                { provide: LessonService, useValue: lessonService },
                { provide: TranslateService, useValue: translateService },
            ],
        });
        facade = TestBed.inject(LessonFacade);

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

    it('loads selected lesson detail and returns null for empty selection', async () => {
        expect(facade.selectedLesson()).toBeNull();

        facade.loadLesson('lesson-1');
        await waitForAsync(() => facade.selectedLesson() !== null);

        expect(lessonService.getById).toHaveBeenCalledWith('lesson-1');
        expect(facade.selectedLesson()).toEqual(createDetail());
    });

    it('marks loaded lessons and selected lesson as read after service succeeds', async () => {
        facade.loadLesson('lesson-1');
        await waitForAsync(() => facade.selectedLesson() !== null);

        facade.markRead('lesson-1');
        await waitForAsync(() => facade.selectedLesson()?.isRead === true);

        expect(lessonService.markRead).toHaveBeenCalledWith('lesson-1');
        expect(facade.selectedLesson()?.isRead).toBe(true);
        expect(facade.lessons().at(0)?.isRead).toBe(true);
    });
});

describe('LessonFacade refresh state', () => {
    beforeEach(setupFacade);

    it('keeps the last loaded page while filters trigger a new request', async () => {
        await waitForAsync(() => facade.lessons().length > 0);
        const nextPage = new Subject<LessonPage>();
        lessonService.getAll.mockReturnValue(nextPage);

        facade.loadLessons('Micronutrients');
        await waitForAsync(() => facade.isLoading());

        expect(facade.lessons()).toEqual([createSummary()]);
        expect(facade.page().availableCategories).toEqual(['Macronutrients']);

        nextPage.next({
            ...createPage(),
            items: [{ ...createSummary(), id: 'lesson-2', category: 'Micronutrients' }],
            availableCategories: ['Macronutrients', 'Micronutrients'],
        });
        nextPage.complete();
        await waitForAsync(() => facade.lessons().at(0)?.id === 'lesson-2');

        expect(facade.page().availableCategories).toEqual(['Macronutrients', 'Micronutrients']);
    });
});

function setupFacade(): void {
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
            { provide: LessonService, useValue: lessonService },
            { provide: TranslateService, useValue: translateService },
        ],
    });

    facade = TestBed.inject(LessonFacade);
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
        id: 'lesson-1',
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
