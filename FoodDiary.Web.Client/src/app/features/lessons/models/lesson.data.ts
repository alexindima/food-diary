export interface LessonSummary {
    id: string;
    title: string;
    summary?: string | null;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    isRead: boolean;
}

export interface LessonDetail {
    id: string;
    title: string;
    content: string;
    summary?: string | null;
    category: string;
    difficulty: string;
    estimatedReadMinutes: number;
    isRead: boolean;
}

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

export type LessonCategoryKey = (typeof LESSON_CATEGORIES)[number];
