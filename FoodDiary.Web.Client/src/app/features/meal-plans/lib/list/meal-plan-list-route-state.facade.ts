import { inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map, tap } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type MealPlanListQuery, mealPlanListQueryKey, readMealPlanListQuery, writeMealPlanListQuery } from './meal-plan-list-query';
import type { MealPlanListQueryState } from './meal-plan-list-query-state';

@Injectable()
export class MealPlanListRouteStateFacade implements MealPlanListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private pendingKey: string | null = null;
    public readonly initial = readMealPlanListQuery(this.route.snapshot.queryParamMap);
    public readonly current = signal(this.initial);
    public readonly changes = this.route.queryParamMap.pipe(
        map(readMealPlanListQuery),
        distinctUntilChanged((a, b) => mealPlanListQueryKey(a) === mealPlanListQueryKey(b)),
        tap(query => {
            if (mealPlanListQueryKey(query) !== mealPlanListQueryKey(this.current())) {
                this.current.set(query);
            }
        }),
    );
    public async normalizePageAsync(): Promise<boolean> {
        return (
            hasInvalidPaginationPage(this.route.snapshot.queryParamMap.get('page')) && this.writeAsync(this.current(), { replaceUrl: true })
        );
    }
    public async writeAsync(query: MealPlanListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        const key = mealPlanListQueryKey(query);
        if (
            this.pendingKey === key ||
            (key === mealPlanListQueryKey(readMealPlanListQuery(this.route.snapshot.queryParamMap)) &&
                !hasInvalidPaginationPage(this.route.snapshot.queryParamMap.get('page')))
        ) {
            return false;
        }
        this.current.set(query);
        this.pendingKey = key;
        try {
            return await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: writeMealPlanListQuery(query),
                queryParamsHandling: 'merge',
                replaceUrl: options?.replaceUrl ?? false,
            });
        } finally {
            if (this.pendingKey === key) {
                this.pendingKey = null;
            }
        }
    }
}
