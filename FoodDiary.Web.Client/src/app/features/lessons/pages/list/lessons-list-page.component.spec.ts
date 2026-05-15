import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { LessonFacade } from '../../lib/lesson.facade';
import type { LessonSummary } from '../../models/lesson.data';
import { LessonsListPageComponent } from './lessons-list-page.component';

describe('LessonsListPageComponent', () => {
    it('loads lessons on creation and maps list state', () => {
        const facade = createFacadeStub([createSummary({ isRead: true })]);
        const component = createComponent(facade);

        expect(facade.loadLessons).toHaveBeenCalledWith();
        expect(component.progress()).toEqual({ read: 1, total: 1, percent: 100 });
        expect(component.lessons()[0]).toMatchObject({
            categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
            difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
        });
    });

    it('filters lessons by selected category', () => {
        const facade = createFacadeStub();
        const component = createComponent(facade);

        component.filterByCategory('Hydration');

        expect(facade.loadLessons).toHaveBeenLastCalledWith('Hydration');
    });

    it('navigates to selected lesson detail', () => {
        const facade = createFacadeStub();
        const router = createRouterStub();
        const component = createComponent(facade, router);

        component.openLesson('lesson-1');

        expect(router.navigate).toHaveBeenCalledWith(['/lessons', 'lesson-1']);
    });
});

type FacadeStub = {
    categoryFilter: ReturnType<typeof signal<string | null>>;
    lessons: ReturnType<typeof signal<LessonSummary[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    loadLessons: ReturnType<typeof vi.fn<(category?: string | null) => void>>;
};

function createComponent(facade: FacadeStub, router = createRouterStub()): LessonsListPageComponent {
    TestBed.configureTestingModule({
        providers: [
            { provide: LessonFacade, useValue: facade },
            { provide: Router, useValue: router },
        ],
    });

    return TestBed.runInInjectionContext(() => new LessonsListPageComponent());
}

function createRouterStub(): { navigate: ReturnType<typeof vi.fn<(commands: string[]) => Promise<boolean>>> } {
    return {
        navigate: vi.fn(async () => {
            await Promise.resolve();
            return true;
        }),
    };
}

function createFacadeStub(lessons: LessonSummary[] = []): FacadeStub {
    return {
        categoryFilter: signal<string | null>(null),
        lessons: signal(lessons),
        isLoading: signal(false),
        loadLessons: vi.fn(),
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
