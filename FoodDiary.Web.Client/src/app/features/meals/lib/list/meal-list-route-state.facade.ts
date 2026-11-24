import { computed, inject, Injectable, type Signal, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { distinctUntilChanged, startWith } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type MealListQuery, mealListQueryKey, readMealListQuery, writeMealListQuery } from './meal-list-query';

@Injectable()
export class MealListRouteStateFacade {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly boundQuery = signal<Signal<MealListQuery> | null>(null);
    public readonly initial = readMealListQuery(convertToParamMap(this.route.snapshot.queryParams));
    public readonly changes = toObservable(computed(() => this.boundQuery()?.() ?? this.initial)).pipe(
        // The initial overview may resolve and correct the page before bound inputs emit.
        startWith(this.initial),
        distinctUntilChanged((left, right) => mealListQueryKey(left) === mealListQueryKey(right)),
    );

    public bind(query: Signal<MealListQuery>): void {
        this.boundQuery.set(query);
    }

    public async writeAsync(query: MealListQuery, replaceUrl = false): Promise<boolean> {
        const committed = convertToParamMap(this.route.snapshot.queryParams);
        if (
            mealListQueryKey(query) === mealListQueryKey(readMealListQuery(committed)) &&
            !hasInvalidPaginationPage(committed.get('page'))
        ) {
            return false;
        }
        return this.router.navigate([], {
            relativeTo: this.route,
            queryParams: writeMealListQuery(query),
            queryParamsHandling: 'merge',
            replaceUrl,
        });
    }
}
