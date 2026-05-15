import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { LESSON_CATEGORIES, type LessonDetail, type LessonSummary } from '../models/lesson.data';

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
} & LessonSummary;

export type LessonDetailViewModel = {
    categoryLabelKey: string;
    difficultyLabelKey: string;
} & LessonDetail;

const CATEGORY_DEFINITIONS = [
    { value: null, labelKey: 'LESSONS.FILTER_ALL' },
    ...LESSON_CATEGORIES.map(category => ({
        value: category,
        labelKey: `LESSONS.CATEGORY.${category}`,
    })),
];

export function buildLessonCategoryOptions(selectedCategory: string | null): LessonCategoryOption[] {
    return CATEGORY_DEFINITIONS.map(category => ({
        ...category,
        fill: selectedCategory === category.value ? 'solid' : 'outline',
    }));
}

export function buildLessonProgress(lessons: LessonSummary[]): LessonProgressViewModel | null {
    if (lessons.length === 0) {
        return null;
    }

    const read = lessons.filter(lesson => lesson.isRead).length;
    return {
        read,
        total: lessons.length,
        percent: Math.round((read / lessons.length) * PERCENT_MULTIPLIER),
    };
}

export function buildLessonListItems(lessons: LessonSummary[]): LessonListItemViewModel[] {
    return lessons.map(lesson => ({
        ...lesson,
        categoryLabelKey: buildLessonCategoryLabelKey(lesson.category),
        difficultyLabelKey: buildLessonDifficultyLabelKey(lesson.difficulty),
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
