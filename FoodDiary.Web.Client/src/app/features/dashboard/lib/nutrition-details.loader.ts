import { EMPTY, expand, map, type Observable, reduce } from 'rxjs';

import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import type { Meal } from '../../../shared/models/meal.data';
import type { DashboardSnapshotQuery } from '../api/dashboard.service';

export type NutritionDay = { snapshot: DashboardSnapshot; meals: Meal[] };
const PAGE_SIZE = 100;

/** Fetches every page before exposing a breakdown, and cancels with its subscriber. */
export function loadNutritionDay(
    query: DashboardSnapshotQuery,
    request: (query: DashboardSnapshotQuery) => Observable<DashboardSnapshot>,
): Observable<NutritionDay> {
    const fetchPage = (page: number): Observable<{ page: number; snapshot: DashboardSnapshot }> =>
        request({ ...query, page, pageSize: PAGE_SIZE }).pipe(map(snapshot => ({ page, snapshot })));
    return fetchPage(1).pipe(
        expand(({ page, snapshot }) => {
            if (snapshot.meals.items.length === 0 && snapshot.meals.total > (page - 1) * PAGE_SIZE) {
                throw new Error('Incomplete nutrition day');
            }
            return page * PAGE_SIZE < snapshot.meals.total ? fetchPage(page + 1) : EMPTY;
        }, 1),
        reduce((state: NutritionDay | null, { snapshot }) => {
            if (
                state !== null &&
                (state.snapshot.meals.total !== snapshot.meals.total ||
                    JSON.stringify(state.snapshot.statistics) !== JSON.stringify(snapshot.statistics))
            ) {
                throw new Error('Nutrition day changed while loading');
            }
            return { snapshot: state?.snapshot ?? snapshot, meals: [...(state?.meals ?? []), ...snapshot.meals.items] };
        }, null),
        map(state => {
            if (
                state === null ||
                state.meals.length !== state.snapshot.meals.total ||
                new Set(state.meals.map(meal => meal.id)).size !== state.meals.length
            ) {
                throw new Error('Incomplete nutrition day');
            }
            return state;
        }),
    );
}
