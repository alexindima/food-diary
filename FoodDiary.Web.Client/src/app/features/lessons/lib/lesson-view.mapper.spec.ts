import { describe, expect, it } from 'vitest';

import type { LessonDetail, LessonSummary } from '../models/lesson.data';
import { buildLessonCategoryOptions, buildLessonDetailView, buildLessonListItems, buildLessonProgress } from './lesson-view.mapper';

describe('lesson view mapper', () => {
    it('builds category options with selected fill state', () => {
        const options = buildLessonCategoryOptions(['Hydration', 'Macronutrients'], 'Hydration');

        expect(options[0]).toEqual({
            value: null,
            labelKey: 'LESSONS.FILTER_ALL',
            fill: 'outline',
        });
        expect(options.find(option => option.value === 'Hydration')?.fill).toBe('solid');
        expect(options.find(option => option.value === 'Macronutrients')?.fill).toBe('outline');
        expect(options.find(option => option.value === 'NutritionBasics')).toBeUndefined();
    });

    it('builds progress for read lessons', () => {
        const progress = buildLessonProgress(1, 2);

        expect(progress).toEqual({ read: 1, total: 2, percent: 50 });
    });

    it('returns null progress for empty lessons', () => {
        expect(buildLessonProgress(0, 0)).toBeNull();
    });

    it('builds list item translation keys', () => {
        const items = buildLessonListItems([createSummary({ category: 'Macronutrients', difficulty: 'Beginner' })]);

        expect(items[0]).toMatchObject({
            categoryLabelKey: 'LESSONS.CATEGORY.Macronutrients',
            difficultyLabelKey: 'LESSONS.DIFFICULTY.Beginner',
            difficultyLevel: 1,
        });
    });

    it('builds detail view translation keys', () => {
        const view = buildLessonDetailView(createDetail({ category: 'Hydration', difficulty: 'Advanced' }));

        expect(view).toMatchObject({
            categoryLabelKey: 'LESSONS.CATEGORY.Hydration',
            difficultyLabelKey: 'LESSONS.DIFFICULTY.Advanced',
        });
    });

    it('returns null detail view for missing lesson', () => {
        expect(buildLessonDetailView(null)).toBeNull();
    });
});

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

function createDetail(overrides: Partial<LessonDetail> = {}): LessonDetail {
    return {
        ...createSummary(),
        content: 'Lesson content',
        ...overrides,
    };
}
