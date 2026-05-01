export type AdminLesson = {
    id: string;
    title: string;
    content: string;
    summary: string | null;
    locale: string;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    sortOrder: number;
    createdOnUtc: string;
    modifiedOnUtc: string | null;
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
