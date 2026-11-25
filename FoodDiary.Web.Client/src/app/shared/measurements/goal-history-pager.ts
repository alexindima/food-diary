import { computed, effect, type Signal, signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { type Observable, tap } from 'rxjs';

import type { GoalHistoryPage } from '../models/user.data';

export class GoalHistoryPager<T extends { id: string }> {
    public readonly items = signal<T[]>([]);
    public readonly loaded = signal(false);
    private readonly cursor = signal<string | null>(null);
    private readonly requestedPage = signal<{ cursor: string | undefined } | undefined>(undefined);
    private readonly pageResource = rxResource({
        params: this.requestedPage,
        stream: ({ params, abortSignal }) =>
            this.fetchPage(params.cursor).pipe(
                tap(page => {
                    if (!abortSignal.aborted && this.requestedPage() === params) {
                        this.appendPage(page);
                    }
                }),
            ),
    });
    public readonly loading = this.pageResource.isLoading;
    public readonly failed = computed(() => this.pageResource.error() !== undefined);
    // Each dialog owns its pages; closing it releases both rows and pending HTTP work.
    public readonly hasMore = computed(() => !this.loaded() || this.cursor() !== null);

    public constructor(
        private readonly fetchPage: (cursor?: string) => Observable<GoalHistoryPage<T>>,
        revision: Signal<number>,
    ) {
        effect(() => {
            revision();
            untracked(() => {
                this.reset();
            });
        });
    }

    public loadMore(): void {
        if (this.loading() || !this.hasMore()) {
            return;
        }
        this.requestedPage.set({ cursor: this.cursor() ?? undefined });
    }

    private appendPage(page: GoalHistoryPage<T>): void {
        const byId = new Map(this.items().map(item => [item.id, item]));
        for (const item of page.items) {
            byId.set(item.id, item);
        }
        this.items.set([...byId.values()]);
        this.cursor.set(page.nextCursor);
        this.loaded.set(true);
    }

    private reset(): void {
        this.requestedPage.set(undefined);
        this.pageResource.set(undefined);
        this.items.set([]);
        this.cursor.set(null);
        this.loaded.set(false);
        this.loadMore();
    }
}
