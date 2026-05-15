import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { LessonDetailViewModel } from '../../../../lib/lesson-view.mapper';
import { LessonDetailContentComponent } from './lesson-detail-content.component';

describe('LessonDetailContentComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LessonDetailContentComponent, TranslateModule.forRoot()],
        });
    });

    it('renders loader while loading', () => {
        const fixture = createComponent({ isLoading: true, lesson: createLesson() });
        const element = getElement(fixture);

        expect(element.querySelector('fd-ui-loader')).not.toBeNull();
        expect(element.querySelector('.lesson-detail__article')).toBeNull();
    });

    it('renders unread lesson and emits mark read action', () => {
        const fixture = createComponent({ lesson: createLesson({ isRead: false }) });
        const markRead = vi.fn();
        fixture.componentInstance.markRead.subscribe(markRead);
        const element = getElement(fixture);

        element.querySelector<HTMLElement>('fd-ui-button')?.click();

        expect(element.textContent).toContain('Macros');
        expect(element.textContent).toContain('Lesson content');
        expect(markRead).toHaveBeenCalled();
    });

    it('renders read badge for completed lesson', () => {
        const fixture = createComponent({ lesson: createLesson({ isRead: true }) });
        const element = getElement(fixture);

        expect(element.querySelector('.lesson-detail__read-badge')).not.toBeNull();
        expect(element.querySelector('fd-ui-button')).toBeNull();
    });
});

function createComponent(
    overrides: Partial<{ isLoading: boolean; lesson: LessonDetailViewModel | null }> = {},
): ComponentFixture<LessonDetailContentComponent> {
    const fixture = TestBed.createComponent(LessonDetailContentComponent);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('lesson', overrides.lesson ?? createLesson());
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<LessonDetailContentComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createLesson(overrides: Partial<LessonDetailViewModel> = {}): LessonDetailViewModel {
    return {
        id: 'lesson-1',
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: 5,
        isRead: false,
        content: 'Lesson content',
        categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
        difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
        ...overrides,
    };
}
