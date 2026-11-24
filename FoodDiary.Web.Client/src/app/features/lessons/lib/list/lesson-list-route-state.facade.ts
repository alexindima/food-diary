import { computed, inject, Injectable, type Signal, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { distinctUntilChanged, tap } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type LessonListQuery, lessonListQueryKey, readLessonListQuery, writeLessonListQuery } from './lesson-list-query';
import type { LessonListQueryState } from './lesson-list-query-state';

@Injectable()
export class LessonListRouteStateFacade implements LessonListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly boundQuery = signal<Signal<LessonListQuery> | null>(null);
    private pendingKey: string | null = null;
    public readonly initial = this.readCommittedQuery();
    private committedKey = lessonListQueryKey(this.initial);
    public readonly current = signal(this.initial);
    public readonly changes = toObservable(computed(() => this.boundQuery()?.() ?? this.initial)).pipe(
        distinctUntilChanged((previous, query) => {
            const key = lessonListQueryKey(query);
            return key === lessonListQueryKey(previous) && key === lessonListQueryKey(this.current());
        }),
        tap(query => {
            const key = lessonListQueryKey(query);
            this.committedKey = key;
            if (key !== lessonListQueryKey(this.current())) {
                this.current.set(query);
            }
        }),
    );
    public bind(query: Signal<LessonListQuery>): void {
        this.boundQuery.set(query);
    }
    public isCurrent(query: LessonListQuery): boolean {
        const key = lessonListQueryKey(query);
        const committedKey = lessonListQueryKey(this.readCommittedQuery());
        // Input binding can trail router navigation. Only the current or the last observed URL may still own this result.
        return key === lessonListQueryKey(this.current()) && (key === committedKey || this.committedKey === committedKey);
    }
    public async normalizePageAsync(): Promise<boolean> {
        return this.hasInvalidCommittedPage() && this.writeAsync(this.current(), { replaceUrl: true });
    }
    public async writeAsync(query: LessonListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        const key = lessonListQueryKey(query);
        const committedKey = lessonListQueryKey(this.readCommittedQuery());
        if (this.pendingKey === key || (key === committedKey && !this.hasInvalidCommittedPage())) {
            return false;
        }
        this.committedKey = committedKey;
        this.current.set(query);
        this.pendingKey = key;
        try {
            const navigated = await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: writeLessonListQuery(query),
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

    private readCommittedQuery(): LessonListQuery {
        return readLessonListQuery(convertToParamMap(this.route.snapshot.queryParams));
    }

    private hasInvalidCommittedPage(): boolean {
        return hasInvalidPaginationPage(convertToParamMap(this.route.snapshot.queryParams).get('page'));
    }
}
