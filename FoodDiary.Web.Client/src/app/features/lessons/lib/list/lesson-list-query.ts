import type { ParamMap } from '@angular/router';

import { readPaginationPage } from '../../../../shared/navigation/pagination-query.utils';

const CATEGORIES = new Set([
    'NutritionBasics',
    'Macronutrients',
    'Micronutrients',
    'MealTiming',
    'MindfulEating',
    'WeightManagement',
    'Hydration',
    'FoodQuality',
    'CookingTips',
]);
const DIFFICULTIES = new Set(['Beginner', 'Intermediate', 'Advanced']);

export type LessonListQuery = {
    page: number;
    search: string;
    category: string | null;
    difficulty: string | null;
    sort: 'recommended' | 'shortest';
};
export function readLessonListQuery(params: ParamMap): LessonListQuery {
    const category = params.get('category');
    const difficulty = params.get('difficulty');
    return {
        page: readPaginationPage(params.get('page')),
        search: params.get('search')?.trim() ?? '',
        category: category !== null && CATEGORIES.has(category) ? category : null,
        difficulty: difficulty !== null && DIFFICULTIES.has(difficulty) ? difficulty : null,
        sort: params.get('sort') === 'shortest' ? 'shortest' : 'recommended',
    };
}
export function writeLessonListQuery(query: LessonListQuery): Record<string, string | null> {
    return {
        page: query.page === 1 ? null : String(query.page),
        search: query.search.trim().length > 0 ? query.search.trim() : null,
        category: query.category,
        difficulty: query.difficulty,
        sort: query.sort === 'recommended' ? null : query.sort,
    };
}
export function lessonListQueryKey(query: LessonListQuery): string {
    return JSON.stringify(writeLessonListQuery(query));
}
