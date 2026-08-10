import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { LessonFacade } from '../../lib/lesson.facade';
import type { LessonSummary } from '../../models/lesson.data';
import { LessonsListPageComponent } from './lessons-list-page';

describe('LessonsListPageComponent', () => {
    it('loads lessons on creation and maps list state', () => {
        const facade = createFacadeStub([createSummary({ isRead: true })]);
        const component = createComponent(facade);

        expect(facade.loadLessons).toHaveBeenCalledWith();
        expect(component['progress']()).toEqual({ read: 1, total: 1, percent: 100 });
        expect(component['lessons']()[0]).toMatchObject({
            categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
            difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
            difficultyLevel: 1,
        });
    });

    it('updates server category filter and resets pagination', () => {
        const facade = createFacadeStub([createSummary({ category: 'Hydration' }), createSummary({ id: 'lesson-2' })]);
        const component = createComponent(facade);

        component['filterByCategory']('Hydration');

        expect(facade.categoryFilter()).toBe('Hydration');
        expect(facade.resetPage).toHaveBeenCalledOnce();
        expect(facade.loadLessons).toHaveBeenCalledTimes(1);
    });

    it('updates server search query', () => {
        const facade = createFacadeStub([
            createSummary({ title: 'Protein', summary: 'Muscle recovery' }),
            createSummary({ id: 'lesson-2', title: 'Hydration', summary: 'Water balance' }),
        ]);
        const component = createComponent(facade);

        component['updateSearch']('water');

        expect(facade.searchQuery()).toBe('water');
        expect(facade.resetPage).toHaveBeenCalledOnce();
    });

    it('navigates to selected lesson detail', () => {
        const facade = createFacadeStub();
        const router = createRouterStub();
        const component = createComponent(facade, router);

        component['openLesson']('lesson-1');

        expect(router.navigate).toHaveBeenCalledWith(['/lessons', 'lesson-1']);
    });
});

type FacadeStub = {
    categoryFilter: ReturnType<typeof signal<string | null>>;
    difficultyFilter: ReturnType<typeof signal<string | null>>;
    searchQuery: ReturnType<typeof signal<string>>;
    sortOrder: ReturnType<typeof signal<'recommended' | 'shortest'>>;
    pageIndex: ReturnType<typeof signal<number>>;
    page: ReturnType<
        typeof signal<{
            items: LessonSummary[];
            page: number;
            pageSize: number;
            totalCount: number;
            totalPages: number;
            totalLessonCount: number;
            readLessonCount: number;
        }>
    >;
    lessons: ReturnType<typeof signal<LessonSummary[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    loadLessons: ReturnType<typeof vi.fn<(category?: string | null) => void>>;
    resetPage: ReturnType<typeof vi.fn<() => void>>;
};

function createComponent(facade: FacadeStub, router = createRouterStub()): LessonsListPageComponent {
    TestBed.configureTestingModule({
        providers: [provideTranslateTesting(), { provide: LessonFacade, useValue: facade }, { provide: Router, useValue: router }],
    });

    return TestBed.runInInjectionContext(() => new LessonsListPageComponent());
}

function createRouterStub(): { navigate: ReturnType<typeof vi.fn<(commands: string[]) => Promise<boolean>>> } {
    return {
        navigate: vi.fn(async () => {
            await waitForAsyncTasksAsync();
            return true;
        }),
    };
}

function createFacadeStub(lessons: LessonSummary[] = []): FacadeStub {
    return {
        categoryFilter: signal<string | null>(null),
        difficultyFilter: signal<string | null>(null),
        searchQuery: signal(''),
        sortOrder: signal<'recommended' | 'shortest'>('recommended'),
        pageIndex: signal(0),
        page: signal({
            items: lessons,
            page: 1,
            pageSize: 20,
            totalCount: lessons.length,
            totalPages: lessons.length > 0 ? 1 : 0,
            totalLessonCount: lessons.length,
            readLessonCount: lessons.filter(lesson => lesson.isRead).length,
        }),
        lessons: signal(lessons),
        isLoading: signal(false),
        loadLessons: vi.fn(),
        resetPage: vi.fn(),
    };
}

function createSummary(overrides: Partial<LessonSummary> = {}): LessonSummary {
    return {
        id: 'lesson-1',
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: 5,
        isRead: false,
        ...overrides,
    };
}
