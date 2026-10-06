import type { ParamMap } from '@angular/router';

import { readPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import type { DietType } from '../../models/meal-plan.data';

const DIETS: readonly DietType[] = ['Balanced', 'HighProtein', 'LowCarb', 'Keto', 'Mediterranean', 'Vegan', 'Vegetarian'];
export type MealPlanListQuery = { page: number; dietType: DietType | null };
export function readMealPlanListQuery(params: ParamMap): MealPlanListQuery {
    const dietType = params.get('dietType');
    return { page: readPaginationPage(params.get('page')), dietType: DIETS.find(value => value === dietType) ?? null };
}
export function writeMealPlanListQuery(query: MealPlanListQuery): Record<string, string | null> {
    return { page: query.page === 1 ? null : String(query.page), dietType: query.dietType };
}
export function mealPlanListQueryKey(query: MealPlanListQuery): string {
    return JSON.stringify(writeMealPlanListQuery(query));
}
