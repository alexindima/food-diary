import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { entityId } from '../../../../../../shared/models/semantics/entity-id';
import type { LessonListItemViewModel } from '../../../../lib/lesson-view.mapper';
import { LessonsListGridComponent } from './lessons-list-grid';

describe('LessonsListGridComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LessonsListGridComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('renders loader while loading', () => {
        const fixture = createComponent({ isLoading: true, lessons: [createLesson()] });
        const element = getElement(fixture);

        expect(element.querySelector('fd-ui-loader')).not.toBeNull();
        expect(element.querySelector('.lesson-card')).toBeNull();
    });

    it('renders empty state when lessons are empty', () => {
        const fixture = createComponent({ lessons: [] });
        const element = getElement(fixture);

        expect(element.querySelector('.lessons-list__empty')).not.toBeNull();
        expect(element.querySelector('.lesson-card')).toBeNull();
    });

    it('renders lessons and emits opened lesson id', () => {
        const fixture = createComponent({ lessons: [createLesson()] });
        const element = getElement(fixture);
        const lessonOpen = vi.fn();
        fixture.componentInstance['lessonOpen'].subscribe(lessonOpen);

        element.querySelector<HTMLElement>('.lesson-card')?.click();

        expect(element.querySelector('.lesson-card__title')?.textContent).toContain('Macros');
        expect(lessonOpen).toHaveBeenCalledWith('lesson-1');
    });

    it('opens a lesson with Space and prevents page scrolling', () => {
        const fixture = createComponent();
        const lessonOpen = vi.fn();
        fixture.componentInstance.lessonOpen.subscribe(lessonOpen);
        const event = new KeyboardEvent('keydown', { key: ' ', cancelable: true });
        getElement(fixture).querySelector('.lesson-card')?.dispatchEvent(event);
        expect(lessonOpen).toHaveBeenCalledWith('lesson-1');
        expect(event.defaultPrevented).toBe(true);
    });
});

function createComponent(
    overrides: Partial<{ isLoading: boolean; lessons: LessonListItemViewModel[] }> = {},
): ComponentFixture<LessonsListGridComponent> {
    const fixture = TestBed.createComponent(LessonsListGridComponent);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('lessons', overrides.lessons ?? [createLesson()]);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<LessonsListGridComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createLesson(): LessonListItemViewModel {
    return {
        id: entityId<'lesson'>('lesson-1'),
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: 5,
        isRead: true,
        categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
        difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
        difficultyLevel: 1,
    };
}
