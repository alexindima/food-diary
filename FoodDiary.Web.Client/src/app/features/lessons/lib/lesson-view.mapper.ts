import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import type { LessonDetail, LessonSummary } from '../models/lesson.data';

const ADVANCED_DIFFICULTY_LEVEL = 3;

export type LessonCategoryOption = {
    value: string | null;
    labelKey: string;
    fill: 'solid' | 'outline';
};

export type LessonProgressViewModel = {
    read: number;
    total: number;
    percent: number;
};

export type LessonListItemViewModel = {
    categoryLabelKey: string;
    difficultyLabelKey: string;
    difficultyLevel: number;
} & LessonSummary;

export type LessonDetailViewModel = {
    categoryLabelKey: string;
    difficultyLabelKey: string;
} & LessonDetail;

export function buildLessonCategoryOptions(categories: readonly string[], selectedCategory: string | null): LessonCategoryOption[] {
    const definitions = [
        { value: null, labelKey: 'LESSONS.FILTER_ALL' },
        ...categories.map(category => ({ value: category, labelKey: `LESSONS.CATEGORY.${category}` })),
    ];

    return definitions.map(category => ({
        ...category,
        fill: selectedCategory === category.value ? 'solid' : 'outline',
    }));
}

export function buildLessonProgress(read: number, total: number): LessonProgressViewModel | null {
    if (total === 0) {
        return null;
    }

    return {
        read,
        total,
        percent: Math.round((read / total) * PERCENT_MULTIPLIER),
    };
}

export function buildLessonListItems(lessons: LessonSummary[]): LessonListItemViewModel[] {
    return lessons.map(lesson => ({
        ...lesson,
        categoryLabelKey: buildLessonCategoryLabelKey(lesson.category),
        difficultyLabelKey: buildLessonDifficultyLabelKey(lesson.difficulty),
        difficultyLevel: buildLessonDifficultyLevel(lesson.difficulty),
    }));
}

export function buildLessonDetailView(lesson: LessonDetail | null): LessonDetailViewModel | null {
    if (lesson === null) {
        return null;
    }

    return {
        ...lesson,
        categoryLabelKey: buildLessonCategoryLabelKey(lesson.category),
        difficultyLabelKey: buildLessonDifficultyLabelKey(lesson.difficulty),
    };
}

function buildLessonCategoryLabelKey(category: string): string {
    return `LESSONS.CATEGORY.${category}`;
}

function buildLessonDifficultyLabelKey(difficulty: string): string {
    return `LESSONS.DIFFICULTY.${difficulty}`;
}

function buildLessonDifficultyLevel(difficulty: string): number {
    switch (difficulty) {
        case 'Beginner': {
            return 1;
        }
        case 'Intermediate': {
            return 2;
        }
        case 'Advanced': {
            return ADVANCED_DIFFICULTY_LEVEL;
        }
        default: {
            return 0;
        }
    }
}
