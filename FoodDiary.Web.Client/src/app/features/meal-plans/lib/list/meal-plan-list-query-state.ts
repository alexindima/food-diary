import { InjectionToken, type Signal } from '@angular/core';
import type { Observable } from 'rxjs';

import type { MealPlanListQuery } from './meal-plan-list-query';
export type MealPlanListQueryState = {
    readonly initial: MealPlanListQuery;
    readonly current: Signal<MealPlanListQuery>;
    readonly changes: Observable<MealPlanListQuery>;
    writeAsync: (query: MealPlanListQuery, options?: { replaceUrl?: boolean }) => Promise<boolean>;
    normalizePageAsync: () => Promise<boolean>;
};
export const MEAL_PLAN_LIST_QUERY_STATE = new InjectionToken<MealPlanListQueryState>('MealPlanListQueryState');
