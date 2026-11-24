import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, type Routes, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdTourService } from 'fd-tour';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { stubScrollIntoView } from '../../../testing/dom-testing';
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

describe('Lessons list controls', () => {
    it('clears a shared search and returns to page one without losing the selected sort', async () => {
        const api = setupLessons([SECOND_LESSON]);
        api.getAll.mockReturnValueOnce(
            of({ ...createPage([FIRST_LESSON]), page: 2, totalPages: 2, totalCount: PAGE_SIZE + 1, totalLessonCount: PAGE_SIZE + 1 }),
        );
        const harness = await RouterTestingHarness.create('/lessons?page=2&search=breakfast&sort=shortest&source=reminder');
        await renderLessonsAsync(harness);
        const route = requireRoute(harness);
        const search = route.querySelector<HTMLInputElement>('input#lessons-search');
        if (search === null) {
            throw new Error('Missing the native lesson search field');
        }
        expect(search.value).toBe('breakfast');
        expect(route.querySelector('fd-lessons-list-grid')?.textContent).toContain(FIRST_LESSON.title);

        search.value = '';
        search.dispatchEvent(new Event('input', { bubbles: true }));
        harness.detectChanges();
        await vi.waitFor(() => {
            expect(api.getAll).toHaveBeenCalledTimes(2);
        });
        await renderLessonsAsync(harness);

        expect(api.getAll.mock.calls.map(([query]) => [query.page, query.search, query.sort])).toEqual([
            [2, 'breakfast', 'shortest'],
            [1, undefined, 'shortest'],
        ]);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ sort: 'shortest', source: 'reminder' });
        expect(search.value).toBe('');
        const grid = route.querySelector('fd-lessons-list-grid');
        expect(grid?.textContent).toContain(SECOND_LESSON.title);
        expect(grid?.textContent).not.toContain(FIRST_LESSON.title);
    });

    it('resets the page for a difficulty change and keeps the search while changing sort and clearing difficulty', async () => {
        stubScrollIntoView();
        const api = setupLessons([FIRST_LESSON]);
        api.getAll.mockReturnValueOnce(
            of({ ...createPage([FIRST_LESSON]), page: 2, totalPages: 2, totalCount: PAGE_SIZE + 1, totalLessonCount: PAGE_SIZE + 1 }),
        );
        const harness = await RouterTestingHarness.create('/lessons?page=2&search=breakfast');
        await renderLessonsAsync(harness);
        expect(api.getAll).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, search: 'breakfast', sort: 'recommended' }));

        await selectLessonOptionAsync(harness, 'LESSONS.LEVEL_FILTER', 'LESSONS.DIFFICULTY.Beginner');
        expect(api.getAll).toHaveBeenLastCalledWith(
            expect.objectContaining({ page: 1, search: 'breakfast', difficulty: 'Beginner', sort: 'recommended' }),
        );

        await selectLessonOptionAsync(harness, 'LESSONS.SORT', 'LESSONS.SORT_SHORTEST');
        expect(api.getAll).toHaveBeenLastCalledWith(
            expect.objectContaining({ page: 1, search: 'breakfast', difficulty: 'Beginner', sort: 'shortest' }),
        );

        await selectLessonOptionAsync(harness, 'LESSONS.LEVEL_FILTER', 'LESSONS.ALL_LEVELS');
        expect(api.getAll).toHaveBeenLastCalledWith(
            expect.objectContaining({ page: 1, search: 'breakfast', difficulty: undefined, sort: 'shortest' }),
        );
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'breakfast', sort: 'shortest' });

        await selectLessonOptionAsync(harness, 'LESSONS.SORT', 'LESSONS.SORT_RECOMMENDED');
        expect(api.getAll).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, search: 'breakfast', sort: 'recommended' }));
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'breakfast' });
    });

    it('replays the lessons tour from the list header', async () => {
        setupLessons();
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        const harness = await RouterTestingHarness.create('/lessons');
        await renderLessonsAsync(harness);

        requireRoute(harness).querySelector<HTMLButtonElement>('[data-tour-id="lessons-tour-help"] button')?.click();

        expect(start).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'lessons-list' }), { force: true });
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

describe('Lesson detail link recovery and help', () => {
    it('clears the previous article and its pending read action until a valid lesson is opened', async () => {
        const api = setupLessons();
        const previousRead = new Subject<void>();
        api.markRead.mockReturnValueOnce(previousRead);
        const harness = await RouterTestingHarness.create(`/lessons/${FIRST_LESSON.id}`);
        await renderLessonsAsync(harness);
        clickLabel(requireRoute(harness), 'LESSONS.MARK_READ');
        expect(previousRead.observed).toBe(true);

        await harness.navigateByUrl('/lessons/%20');
        await renderLessonsAsync(harness);
        const invalid = requireRoute(harness);
        expect(invalid.querySelector('#lesson-detail-title')).toBeNull();
        expect(invalid.querySelector('[role="alert"]:not([hidden])')?.textContent).toBe('LESSONS.NOT_FOUND');
        expect(previousRead.observed).toBe(false);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(FIRST_LESSON.id);
        previousRead.error(new Error('Previous read action failed'));

        await harness.navigateByUrl(`/lessons/${SECOND_LESSON.id}`);
        await renderLessonsAsync(harness);
        const recovered = requireRoute(harness);
        expect(recovered.querySelector('#lesson-detail-title')?.textContent).toBe(SECOND_LESSON.title);
        expect(recovered.querySelector('[role="alert"]:not([hidden])')).toBeNull();
        clickLabel(recovered, 'LESSONS.MARK_READ');
        await renderLessonsAsync(harness);
        expect(api.markRead.mock.calls).toEqual([[FIRST_LESSON.id], [SECOND_LESSON.id]]);
        expect(recovered.querySelector('.lesson-detail__read-badge')?.textContent).toContain('LESSONS.ALREADY_READ');
    });

    it('opens the lesson detail guide from the visible help action', async () => {
        setupLessons();
        const launchTour = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        const harness = await RouterTestingHarness.create(`/lessons/${FIRST_LESSON.id}`);
        await renderLessonsAsync(harness);

        requireRoute(harness).querySelector<HTMLButtonElement>('[data-tour-id="lesson-detail-tour-help"] button')?.click();

        expect(launchTour).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'lesson-detail' }), { force: true });
    });
});

describe('Lessons on query and route updates', () => {
    it('restores the matching catalogue while returning to its search and cancels the intervening request', async () => {
        const api = setupLessons([FIRST_LESSON]);
        const initialUrl = '/lessons?search=breakfast';
        const harness = await RouterTestingHarness.create(initialUrl);
        await renderLessonsAsync(harness);
        expect(requireRoute(harness).querySelector('.lessons-list__recommendation')?.textContent).toContain(FIRST_LESSON.title);

        const other = new Subject<LessonPage>();
        api.getAll.mockReturnValueOnce(other);
        await harness.navigateByUrl('/lessons?search=hydration');
        await vi.waitFor(() => {
            expect(other.observed).toBe(true);
        });
        harness.detectChanges();
        expect(requireRoute(harness).querySelector('.lessons-list__recommendation')).toBeNull();

        const returning = new Subject<LessonPage>();
        api.getAll.mockReturnValueOnce(returning);
        await harness.navigateByUrl(initialUrl);
        await vi.waitFor(() => {
            expect(returning.observed).toBe(true);
        });
        harness.detectChanges();

        expect(other.observed).toBe(false);
        expect(api.getAll).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'breakfast' }));
        const recommendation = requireRoute(harness).querySelector('.lessons-list__recommendation');
        expect(recommendation?.textContent).toContain(FIRST_LESSON.title);
        expect(recommendation?.getAttribute('aria-busy')).toBe('true');

        returning.next(createPage([FIRST_LESSON]));
        returning.complete();
        await renderLessonsAsync(harness);
        expect(recommendation?.getAttribute('aria-busy')).toBe('false');
    });

    it('keeps a pending read on the selected lesson when only a route parameter changes', async () => {
        const api = setupLessons();
        const pending = new Subject<void>();
        api.markRead.mockReturnValueOnce(pending);
        const initialUrl = `/lessons/${FIRST_LESSON.id}`;
        const harness = await RouterTestingHarness.create(initialUrl);
        await renderLessonsAsync(harness);
        clickLabel(requireRoute(harness), 'LESSONS.MARK_READ');
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(`${initialUrl};source=recommendation`);
        await renderLessonsAsync(harness);

        expect(pending.observed).toBe(true);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(FIRST_LESSON.id);
        expect(api.markRead).toHaveBeenCalledExactlyOnceWith(FIRST_LESSON.id);
        pending.next();
        pending.complete();
        await renderLessonsAsync(harness);
        expect(requireRoute(harness).querySelector('.lesson-detail__read-badge')?.textContent).toContain('LESSONS.ALREADY_READ');
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
            provideRouter(
                [
                    {
                        path: 'lessons',
                        loadChildren: async (): Promise<Routes> => {
                            const { default: routes } = await import('./lesson.routes');
                            return routes;
                        },
                    },
                ],
                withComponentInputBinding(),
            ),
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

async function selectLessonOptionAsync(harness: RouterTestingHarness, controlLabel: string, optionLabel: string): Promise<void> {
    const button = requireRoute(harness).querySelector<HTMLButtonElement>(`fd-ui-select button[aria-label="${controlLabel}"]`);
    if (button === null) {
        throw new Error(`Missing lesson select ${controlLabel}`);
    }
    button.click();
    await renderLessonsAsync(harness);

    const option = Array.from(document.querySelectorAll<HTMLButtonElement>('[role="listbox"] button[role="option"]')).find(
        item => item.textContent.trim() === optionLabel,
    );
    if (option === undefined) {
        throw new Error(`Missing lesson option ${optionLabel}`);
    }
    option.click();
    await renderLessonsAsync(harness);
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
