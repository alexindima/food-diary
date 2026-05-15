import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { LessonListItemViewModel } from '../../../../lib/lesson-view.mapper';
import { LessonsListGridComponent } from './lessons-list-grid.component';

describe('LessonsListGridComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [LessonsListGridComponent, TranslateModule.forRoot()],
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
        fixture.componentInstance.lessonOpen.subscribe(lessonOpen);

        element.querySelector<HTMLElement>('.lesson-card')?.click();

        expect(element.querySelector('.lesson-card__title')?.textContent).toContain('Macros');
        expect(lessonOpen).toHaveBeenCalledWith('lesson-1');
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
        id: 'lesson-1',
        title: 'Macros',
        summary: 'Macro basics',
        category: 'Macronutrients',
        difficulty: 'Beginner',
        estimatedReadMinutes: 5,
        isRead: true,
        categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
        difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
    };
}
