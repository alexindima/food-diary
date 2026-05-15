import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { LessonFacade } from '../../lib/lesson.facade';
import type { LessonDetail } from '../../models/lesson.data';
import { LessonDetailPageComponent } from './lesson-detail-page.component';

describe('LessonDetailPageComponent', () => {
    it('loads lesson from route param and maps detail view', () => {
        const facade = createFacadeStub(createDetail());
        const component = createComponent(facade, 'lesson-1');

        expect(facade.loadLesson).toHaveBeenCalledWith('lesson-1');
        expect(component.lesson()).toMatchObject({
            categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
            difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
        });
    });

    it('does not load lesson when route id is missing', () => {
        const facade = createFacadeStub(null);

        createComponent(facade, null);

        expect(facade.loadLesson).not.toHaveBeenCalled();
    });

    it('marks unread lesson as read', () => {
        const facade = createFacadeStub(createDetail({ isRead: false }));
        const component = createComponent(facade, 'lesson-1');

        component.markRead();

        expect(facade.markRead).toHaveBeenCalledWith('lesson-1');
    });

    it('does not mark already read lesson', () => {
        const facade = createFacadeStub(createDetail({ isRead: true }));
        const component = createComponent(facade, 'lesson-1');

        component.markRead();

        expect(facade.markRead).not.toHaveBeenCalled();
    });

    it('navigates back to lessons list', () => {
        const facade = createFacadeStub(createDetail());
        const router = createRouterStub();
        const component = createComponent(facade, 'lesson-1', router);

        component.goBack();

        expect(router.navigate).toHaveBeenCalledWith(['/lessons']);
    });
});

type FacadeStub = {
    selectedLesson: ReturnType<typeof signal<LessonDetail | null>>;
    isDetailLoading: ReturnType<typeof signal<boolean>>;
    loadLesson: ReturnType<typeof vi.fn<(id: string) => void>>;
    markRead: ReturnType<typeof vi.fn<(id: string) => void>>;
};

function createComponent(facade: FacadeStub, routeId: string | null, router = createRouterStub()): LessonDetailPageComponent {
    TestBed.configureTestingModule({
        providers: [
            { provide: LessonFacade, useValue: facade },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        paramMap: convertToParamMap(routeId === null ? {} : { id: routeId }),
                    },
                },
            },
            { provide: Router, useValue: router },
        ],
    });

    return TestBed.runInInjectionContext(() => new LessonDetailPageComponent());
}

function createRouterStub(): { navigate: ReturnType<typeof vi.fn<(commands: string[]) => Promise<boolean>>> } {
    return {
        navigate: vi.fn(async () => {
            await Promise.resolve();
            return true;
        }),
    };
}

function createFacadeStub(lesson: LessonDetail | null): FacadeStub {
    return {
        selectedLesson: signal(lesson),
        isDetailLoading: signal(false),
        loadLesson: vi.fn(),
        markRead: vi.fn(),
    };
}

function createDetail(overrides: Partial<LessonDetail> = {}): LessonDetail {
    return {
        id: 'lesson-1',
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: 5,
        isRead: false,
        content: 'Lesson content',
        ...overrides,
    };
}
