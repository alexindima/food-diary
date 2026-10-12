import { inject, Injectable } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type MealListQuery, mealListQueryKey, readMealListQuery, writeMealListQuery } from './meal-list-query';

@Injectable()
export class MealListRouteStateFacade {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly initial = readMealListQuery(this.route.snapshot.queryParamMap);
    public readonly changes = this.route.queryParamMap.pipe(
        map(readMealListQuery),
        distinctUntilChanged((left, right) => mealListQueryKey(left) === mealListQueryKey(right)),
    );

    public async writeAsync(query: MealListQuery, replaceUrl = false): Promise<boolean> {
        if (
            mealListQueryKey(query) === mealListQueryKey(readMealListQuery(this.route.snapshot.queryParamMap)) &&
            !hasInvalidPaginationPage(this.route.snapshot.queryParamMap.get('page'))
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
