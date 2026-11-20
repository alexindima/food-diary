import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, type Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { entityId } from '../../shared/models/semantics/entity-id';
import { LessonService } from './api/lesson.service';
import type { LessonDetail, LessonPage, LessonSummary } from './models/lesson.data';

const PAGE_SIZE = 18;
const FIRST_LESSON = createLesson('first-lesson', 'Planning breakfast');
const SECOND_LESSON = createLesson('second-lesson', 'Choosing a snack');
const THIRD_LESSON = createLesson('third-lesson', 'Hydration habits');

describe('Lessons list route', () => {
    it('offers the next unread lesson and links the distinct recommendation to its detail route', async () => {
        const api = setupLessons([{ ...FIRST_LESSON, isRead: true }, SECOND_LESSON, THIRD_LESSON]);
        const harness = await RouterTestingHarness.create('/lessons');
        await renderLessonsAsync(harness);
        const list = requireRoute(harness);
        const progress = list.querySelector('fd-lessons-list-progress');
        const recommendation = list.querySelector('.lessons-list__recommendation');
        expect(progress?.textContent).toContain(SECOND_LESSON.title);
        expect(recommendation?.textContent).toContain(THIRD_LESSON.title);
        if (!(recommendation instanceof HTMLElement)) {
            throw new Error('Expected the recommendation to render');
        }

        clickLabel(recommendation, 'LESSONS.OPEN_ARTICLE');
        await vi.dynamicImportSettled();
        await renderLessonsAsync(harness);

        expect(TestBed.inject(Router).url).toBe(`/lessons/${THIRD_LESSON.id}`);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(THIRD_LESSON.id);
        expect(requireRoute(harness).querySelector('#lesson-detail-title')?.textContent).toBe(THIRD_LESSON.title);
    });

    it('keeps a single unread lesson actionable and clears the recommendation when all lessons are read', async () => {
        const api = setupLessons();
        api.getAll.mockReturnValueOnce(of(createPage([FIRST_LESSON])));
        const harness = await RouterTestingHarness.create('/lessons');
        await renderLessonsAsync(harness);
        expect(requireRoute(harness).querySelector('.lessons-list__recommendation')?.textContent).toContain(FIRST_LESSON.title);

        api.getAll.mockReturnValue(of(createPage([{ ...FIRST_LESSON, isRead: true }])));
        await harness.navigateByUrl('/lessons?sort=shortest');
        await renderLessonsAsync(harness);

        expect(api.getAll).toHaveBeenLastCalledWith(expect.objectContaining({ sort: 'shortest' }));
        expect(requireRoute(harness).querySelector('.lessons-list__recommendation')).toBeNull();
        expect(requireRoute(harness).querySelector('fd-lessons-list-grid')?.textContent).toContain(FIRST_LESSON.title);
    });

    it('retries a failed list in place and keeps the chosen search and sort', async () => {
        const api = setupLessons([SECOND_LESSON]);
        api.getAll.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
        const harness = await RouterTestingHarness.create('/lessons?search=snack&sort=shortest');
        await renderLessonsAsync(harness);
        const page = requireRoute(harness);
        const query = {
            page: 1,
            pageSize: PAGE_SIZE,
            search: 'snack',
            sort: 'shortest',
            locale: 'en',
            category: undefined,
            difficulty: undefined,
        };
        expect(page.querySelector('[role="alert"]')?.textContent).toBe('LESSONS.LOAD_ERROR');
        expect(api.getAll).toHaveBeenCalledExactlyOnceWith(query);

        clickLabel(page, 'ERRORS.RETRY');
        await renderLessonsAsync(harness);

        expect(api.getAll.mock.calls).toEqual([[query], [query]]);
        expect(page.querySelector('[role="alert"]')).toBeNull();
        expect(page.querySelector('fd-lessons-list-grid')?.textContent).toContain(SECOND_LESSON.title);
    });
});

describe('Lesson detail route', () => {
    it('keeps the lesson unread after a failed save and shows the read badge after retry succeeds', async () => {
        const api = setupLessons();
        api.markRead.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
        const harness = await RouterTestingHarness.create(`/lessons/${FIRST_LESSON.id}`);
        await renderLessonsAsync(harness);
        const page = requireRoute(harness);
        clickLabel(page, 'LESSONS.MARK_READ');
        harness.detectChanges();
        expect(page.querySelector('[role="alert"]:not([hidden])')?.textContent).toBe('LESSONS.MARK_READ_ERROR');
        expect(page.querySelector('.lesson-detail__read-badge')).toBeNull();

        clickLabel(page, 'LESSONS.MARK_READ');
        await renderLessonsAsync(harness);

        expect(api.markRead.mock.calls).toEqual([[FIRST_LESSON.id], [FIRST_LESSON.id]]);
        expect(page.querySelector('[role="alert"]:not([hidden])')).toBeNull();
        expect(page.querySelector('.lesson-detail__read-badge')?.textContent).toContain('LESSONS.ALREADY_READ');
    });

    it('clears a pending read action when the same detail route selects a different lesson', async () => {
        const api = setupLessons();
        const pending = new Subject<void>();
        api.markRead.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create(`/lessons/${FIRST_LESSON.id}`);
        await renderLessonsAsync(harness);
        clickLabel(requireRoute(harness), 'LESSONS.MARK_READ');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(`/lessons/${SECOND_LESSON.id}`);
        await renderLessonsAsync(harness);
        expect(pending.observed).toBe(false);
        pending.error(new Error('The previous lesson action failed late'));

        const page = requireRoute(harness);
        expect(page.querySelector('#lesson-detail-title')?.textContent).toBe(SECOND_LESSON.title);
        expect(page.querySelector('[role="alert"]:not([hidden])')).toBeNull();
        expect(page.querySelector('.lesson-detail__read-badge')).toBeNull();
    });

    it('offers return to the list when an old lesson link no longer exists', async () => {
        const api = setupLessons();
        api.getById.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));
        const harness = await RouterTestingHarness.create('/lessons/removed');
        await renderLessonsAsync(harness);
        const page = requireRoute(harness);
        expect(page.querySelector('[role="alert"]')?.textContent).toBe('LESSONS.NOT_FOUND');

        clickLabel(page, 'LESSONS.BACK');
        await vi.dynamicImportSettled();
        await renderLessonsAsync(harness);

        expect(TestBed.inject(Router).url).toBe('/lessons');
        expect(requireRoute(harness).querySelector('fd-lessons-list-grid')).not.toBeNull();
    });
});

type LessonApi = {
    getAll: ReturnType<typeof vi.fn<LessonService['getAll']>>;
    getById: ReturnType<typeof vi.fn<LessonService['getById']>>;
    markRead: ReturnType<typeof vi.fn<LessonService['markRead']>>;
};

function setupLessons(lessons: LessonSummary[] = [FIRST_LESSON, SECOND_LESSON, THIRD_LESSON]): LessonApi {
    const api: LessonApi = {
        getAll: vi.fn<LessonService['getAll']>().mockReturnValue(of(createPage(lessons))),
        getById: vi.fn<LessonService['getById']>(id => {
            const summary = lessons.find(value => value.id === id) ?? FIRST_LESSON;
            const detail: LessonDetail = { ...summary, id, content: '<p>Lesson content</p>' };
            return of(detail);
        }),
        markRead: vi.fn<LessonService['markRead']>().mockReturnValue(of(undefined)),
    };
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                {
                    path: 'lessons',
                    loadChildren: async (): Promise<Routes> => {
                        const { default: routes } = await import('./lesson.routes');
                        return routes;
                    },
                },
            ]),
            provideTranslateTesting(),
            { provide: LessonService, useValue: api },
        ],
    });
    return api;
}

function createLesson(id: string, title: string): LessonSummary {
    const readMinutes = 5;
    return {
        id: entityId<'lesson'>(id),
        title,
        summary: `Read about ${title.toLowerCase()}`,
        category: 'NutritionBasics',
        difficulty: 'Beginner',
        estimatedReadMinutes: readMinutes,
        isRead: false,
    };
}

function createPage(items: LessonSummary[]): LessonPage {
    return {
        items,
        page: 1,
        pageSize: PAGE_SIZE,
        totalCount: items.length,
        totalPages: 1,
        totalLessonCount: items.length,
        readLessonCount: items.filter(item => item.isRead).length,
        availableCategories: ['NutritionBasics'],
    };
}

async function renderLessonsAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function requireRoute(harness: RouterTestingHarness): HTMLElement {
    if (harness.routeNativeElement === null) {
        throw new Error('Expected an active lessons route');
    }
    return harness.routeNativeElement;
}

function clickLabel(root: HTMLElement, label: string): void {
    const button = Array.from(root.querySelectorAll('button')).find(
        control => control.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Missing lesson action ${label}`);
    }
    button.click();
}
