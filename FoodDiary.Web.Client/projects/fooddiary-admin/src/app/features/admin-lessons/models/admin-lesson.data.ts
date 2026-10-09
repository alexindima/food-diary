import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminLesson = {
    id: AdminId<'lesson'>;
    title: string;
    content: string;
    summary: string | null;
    locale: string;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    sortOrder: number;
    isPublished?: boolean;
    createdOnUtc: AdminUtcInstant;
    modifiedOnUtc: AdminUtcInstant | null;
    completedCount?: number;
};

export type AdminLessonCreateRequest = {
    title: string;
    content: string;
    summary: string | null;
    locale: string;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    sortOrder: number;
    isPublished?: boolean;
};

export type AdminLessonUpdateRequest = {
    title: string;
    content: string;
    summary: string | null;
    locale: string;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    sortOrder: number;
    isPublished?: boolean;
};

export type AdminLessonsImportRequest = {
    version: 1;
    lessons: AdminLessonCreateRequest[];
};

export type AdminLessonsImportResponse = {
    importedCount: number;
    lessons: AdminLesson[];
};

export const LESSON_CATEGORIES = [
    'NutritionBasics',
    'Macronutrients',
    'Micronutrients',
    'MealTiming',
    'MindfulEating',
    'WeightManagement',
    'Hydration',
    'FoodQuality',
    'CookingTips',
] as const;

export const LESSON_DIFFICULTIES = ['Beginner', 'Intermediate', 'Advanced'] as const;

export const LESSON_LOCALES = ['ru', 'en'] as const;

export const CONTENT_MAX_LENGTH = 65536;
