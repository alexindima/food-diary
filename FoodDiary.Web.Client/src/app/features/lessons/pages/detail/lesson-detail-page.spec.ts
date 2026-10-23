import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { LessonFacade } from '../../lib/lesson.facade';
import type { LessonDetail } from '../../models/lesson.data';
import { LessonDetailPageComponent } from './lesson-detail-page';

describe('LessonDetailPageComponent', () => {
    it('loads the current lesson when Angular reuses the detail route', async () => {
        const facade = createFacadeStub(null);
        facade.loadLesson.mockImplementation(id => {
            facade.selectedLesson.set(createDetail({ id: entityId<'lesson'>(id), title: id }));
        });
        TestBed.configureTestingModule({
            imports: [LessonDetailPageComponent],
            providers: [provideRouter([{ path: 'lessons/:id', component: LessonDetailPageComponent }])],
        });
        TestBed.overrideComponent(LessonDetailPageComponent, {
            set: { providers: [{ provide: LessonFacade, useValue: facade }], template: '' },
        });
        const harness = await RouterTestingHarness.create();
        const first = await harness.navigateByUrl('/lessons/lesson-1', LessonDetailPageComponent);
        const second = await harness.navigateByUrl('/lessons/lesson-2', LessonDetailPageComponent);

        expect(second).toBe(first);
        expect(facade.loadLesson.mock.calls).toEqual([['lesson-1'], ['lesson-2']]);
        second['markRead']();
        expect(facade.markRead).toHaveBeenCalledWith('lesson-2');
        await harness.navigateByUrl('/lessons/lesson-2?from=notice', LessonDetailPageComponent);
        expect(facade.loadLesson).toHaveBeenCalledTimes(2);
    });
    it('loads lesson from route param and maps detail view', () => {
        const facade = createFacadeStub(createDetail());
        const component = createComponent(facade, 'lesson-1');

        expect(facade.loadLesson).toHaveBeenCalledWith('lesson-1');
        expect(component['lesson']()).toMatchObject({
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

        component['markRead']();

        expect(facade.markRead).toHaveBeenCalledWith('lesson-1');
    });

    it('does not mark already read lesson', () => {
        const facade = createFacadeStub(createDetail({ isRead: true }));
        const component = createComponent(facade, 'lesson-1');

        component['markRead']();

        expect(facade.markRead).not.toHaveBeenCalled();
    });

    it('navigates back to lessons list', () => {
        const facade = createFacadeStub(createDetail());
        const router = createRouterStub();
        const component = createComponent(facade, 'lesson-1', router);

        component['goBack']();

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
                    paramMap: of(convertToParamMap(routeId === null ? {} : { id: routeId })),
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
            await waitForAsyncTasksAsync();
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
        id: entityId<'lesson'>('lesson-1'),
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
