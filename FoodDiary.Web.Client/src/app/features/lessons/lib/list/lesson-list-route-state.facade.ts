import { inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map, tap } from 'rxjs';

import { hasInvalidPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { type LessonListQuery, lessonListQueryKey, readLessonListQuery, writeLessonListQuery } from './lesson-list-query';
import type { LessonListQueryState } from './lesson-list-query-state';

@Injectable()
export class LessonListRouteStateFacade implements LessonListQueryState {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private pendingKey: string | null = null;
    public readonly initial = readLessonListQuery(this.route.snapshot.queryParamMap);
    public readonly current = signal(this.initial);
    public readonly changes = this.route.queryParamMap.pipe(
        map(readLessonListQuery),
        distinctUntilChanged((a, b) => lessonListQueryKey(a) === lessonListQueryKey(b)),
        tap(query => {
            if (lessonListQueryKey(query) !== lessonListQueryKey(this.current())) {
                this.current.set(query);
            }
        }),
    );
    public async normalizePageAsync(): Promise<boolean> {
        return (
            hasInvalidPaginationPage(this.route.snapshot.queryParamMap.get('page')) && this.writeAsync(this.current(), { replaceUrl: true })
        );
    }
    public async writeAsync(query: LessonListQuery, options?: { replaceUrl?: boolean }): Promise<boolean> {
        const key = lessonListQueryKey(query);
        if (
            this.pendingKey === key ||
            (key === lessonListQueryKey(readLessonListQuery(this.route.snapshot.queryParamMap)) &&
                !hasInvalidPaginationPage(this.route.snapshot.queryParamMap.get('page')))
        ) {
            return false;
        }
        this.current.set(query);
        this.pendingKey = key;
        try {
            return await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: writeLessonListQuery(query),
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
