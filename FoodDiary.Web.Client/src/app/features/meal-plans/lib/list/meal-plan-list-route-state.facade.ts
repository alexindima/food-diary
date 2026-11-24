import { computed, inject, Injectable, type Signal, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { distinctUntilChanged, tap } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type MealPlanListQuery, mealPlanListQueryKey, readMealPlanListQuery, writeMealPlanListQuery } from './meal-plan-list-query';
import type { MealPlanListQueryState } from './meal-plan-list-query-state';

@Injectable()
export class MealPlanListRouteStateFacade implements MealPlanListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly boundQuery = signal<Signal<MealPlanListQuery> | null>(null);
    private pendingKey: string | null = null;
    public readonly initial = this.readCommittedQuery();
    private committedKey = mealPlanListQueryKey(this.initial);
    public readonly current = signal(this.initial);
    public readonly changes = toObservable(computed(() => this.boundQuery()?.() ?? this.initial)).pipe(
        distinctUntilChanged((previous, query) => {
            const key = mealPlanListQueryKey(query);
            return key === mealPlanListQueryKey(previous) && key === mealPlanListQueryKey(this.current());
        }),
        tap(query => {
            const key = mealPlanListQueryKey(query);
            this.committedKey = key;
            if (key !== mealPlanListQueryKey(this.current())) {
                this.current.set(query);
            }
        }),
    );
    public bind(query: Signal<MealPlanListQuery>): void {
        this.boundQuery.set(query);
    }
    public isCurrent(query: MealPlanListQuery): boolean {
        const key = mealPlanListQueryKey(query);
        const committedKey = mealPlanListQueryKey(this.readCommittedQuery());
        // Input binding can trail router navigation. Only the current or the last observed URL may still own this result.
        return key === mealPlanListQueryKey(this.current()) && (key === committedKey || this.committedKey === committedKey);
    }
    public async normalizePageAsync(): Promise<boolean> {
        return this.hasInvalidCommittedPage() && this.writeAsync(this.current(), { replaceUrl: true });
    }
    public async writeAsync(query: MealPlanListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        const key = mealPlanListQueryKey(query);
        const committedKey = mealPlanListQueryKey(this.readCommittedQuery());
        if (this.pendingKey === key || (key === committedKey && !this.hasInvalidCommittedPage())) {
            return false;
        }
        this.committedKey = committedKey;
        this.current.set(query);
        this.pendingKey = key;
        try {
            const navigated = await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: writeMealPlanListQuery(query),
                queryParamsHandling: 'merge',
                replaceUrl: options?.replaceUrl ?? false,
            });
            if (navigated && this.pendingKey === key) {
                this.committedKey = key;
            }
            return navigated;
        } finally {
            if (this.pendingKey === key) {
                this.pendingKey = null;
            }
        }
    }

    private readCommittedQuery(): MealPlanListQuery {
        return readMealPlanListQuery(convertToParamMap(this.route.snapshot.queryParams));
    }

    private hasInvalidCommittedPage(): boolean {
        return hasInvalidPaginationPage(convertToParamMap(this.route.snapshot.queryParams).get('page'));
    }
}
