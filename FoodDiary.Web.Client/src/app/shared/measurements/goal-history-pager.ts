import { computed, DestroyRef, effect, inject, type Signal, signal, untracked } from '@angular/core';
import { finalize, type Observable, type Subscription } from 'rxjs';

import type { GoalHistoryPage } from '../models/user.data';

export class GoalHistoryPager<T extends { id: string }> {
    public readonly items = signal<T[]>([]);
    public readonly loading = signal(false);
    public readonly failed = signal(false);
    public readonly loaded = signal(false);
    private readonly cursor = signal<string | null>(null);
    private readonly destroyRef = inject(DestroyRef);
    private request: Subscription | undefined;
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
        this.destroyRef.onDestroy(() => this.request?.unsubscribe());
    }

    public loadMore(): void {
        if (this.loading() || !this.hasMore()) {
            return;
        }
        this.loading.set(true);
        this.failed.set(false);
        this.request = this.fetchPage(this.cursor() ?? undefined)
            .pipe(
                finalize(() => {
                    this.loading.set(false);
                }),
            )
            .subscribe({
                next: page => {
                    const byId = new Map(this.items().map(item => [item.id, item]));
                    for (const item of page.items) {
                        byId.set(item.id, item);
                    }
                    this.items.set([...byId.values()]);
                    this.cursor.set(page.nextCursor);
                    this.loaded.set(true);
                },
                error: () => {
                    this.failed.set(true);
                },
            });
    }

    private reset(): void {
        this.request?.unsubscribe();
        this.items.set([]);
        this.cursor.set(null);
        this.loaded.set(false);
        this.loadMore();
    }
}
