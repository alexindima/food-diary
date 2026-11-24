import { computed, inject, Injectable, type Signal, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { distinctUntilChanged, tap } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { readRecipeListQuery, type RecipeListQuery, recipeListQueryKey, writeRecipeListQuery } from './recipe-list-query';
import type { RecipeListQueryState } from './recipe-list-query-state';

@Injectable()
export class RecipeListRouteStateFacade implements RecipeListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly boundQuery = signal<Signal<RecipeListQuery> | null>(null);
    private pendingQueryKey: string | null = null;

    public readonly initial = this.readCommittedQuery();
    private committedKey = recipeListQueryKey(this.initial);
    public readonly current = signal(this.initial);
    public readonly changes = toObservable(computed(() => this.boundQuery()?.() ?? this.initial)).pipe(
        distinctUntilChanged((previous, query) => {
            const key = recipeListQueryKey(query);
            return key === recipeListQueryKey(previous) && key === this.committedKey;
        }),
        tap(query => {
            this.committedKey = recipeListQueryKey(query);
            this.current.set(query);
        }),
    );

    public bind(query: Signal<RecipeListQuery>): void {
        this.boundQuery.set(query);
    }

    public isCurrent(query: RecipeListQuery): boolean {
        const key = recipeListQueryKey(query);
        const committedKey = recipeListQueryKey(this.readCommittedQuery());
        // The router's snapshot can advance before component bindings deliver the new query.
        return key === recipeListQueryKey(this.current()) && (key === committedKey || this.committedKey === committedKey);
    }

    public async writeAsync(query: RecipeListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        const key = recipeListQueryKey(query);
        if (
            this.pendingQueryKey === key ||
            (key === recipeListQueryKey(this.readCommittedQuery()) &&
                !hasInvalidPaginationPage(convertToParamMap(this.route.snapshot.queryParams).get('page')))
        ) {
            return false;
        }
        this.current.set(query);
        this.pendingQueryKey = key;
        try {
            const navigated = await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: writeRecipeListQuery(query),
                queryParamsHandling: 'merge',
                replaceUrl: options?.replaceUrl ?? false,
            });
            if (navigated && this.pendingQueryKey === key) {
                this.committedKey = key;
            }
            return navigated;
        } finally {
            if (this.pendingQueryKey === key) {
                this.pendingQueryKey = null;
            }
        }
    }

    private readCommittedQuery(): RecipeListQuery {
        return readRecipeListQuery(convertToParamMap(this.route.snapshot.queryParams));
    }
}
