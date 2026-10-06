import { inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map, tap } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { readRecipeListQuery, type RecipeListQuery, recipeListQueryKey, writeRecipeListQuery } from './recipe-list-query';
import type { RecipeListQueryState } from './recipe-list-query-state';

@Injectable()
export class RecipeListRouteStateFacade implements RecipeListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private pendingQueryKey: string | null = null;

    public readonly initial = readRecipeListQuery(this.route.snapshot.queryParamMap);
    public readonly current = signal(this.initial);
    public readonly changes = this.route.queryParamMap.pipe(
        map(readRecipeListQuery),
        distinctUntilChanged((left, right) => recipeListQueryKey(left) === recipeListQueryKey(right)),
        tap(query => {
            this.current.set(query);
        }),
    );

    public async writeAsync(query: RecipeListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        const key = recipeListQueryKey(query);
        if (
            this.pendingQueryKey === key ||
            (key === recipeListQueryKey(readRecipeListQuery(this.route.snapshot.queryParamMap)) &&
                !hasInvalidPaginationPage(this.route.snapshot.queryParamMap.get('page')))
        ) {
            return false;
        }
        this.current.set(query);
        this.pendingQueryKey = key;
        try {
            return await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: writeRecipeListQuery(query),
                queryParamsHandling: 'merge',
                replaceUrl: options?.replaceUrl ?? false,
            });
        } finally {
            if (this.pendingQueryKey === key) {
                this.pendingQueryKey = null;
            }
        }
    }
}
