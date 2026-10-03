import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import type { Subscription } from 'rxjs';

import type { ShoppingListPage, ShoppingListSummary } from '../../../shared/models/shopping-list.data';
import { ShoppingListService } from '../api/shopping-list.service';

const PAGE_SIZE = 20;
@Injectable()
export class ShoppingListBrowserFacade {
    private readonly service = inject(ShoppingListService);
    private readonly destroyRef = inject(DestroyRef);
    private request: Subscription | undefined;
    private page = 1;
    private query = '';
    public readonly lists = signal<ShoppingListSummary[]>([]);
    public readonly loading = signal(false);
    public readonly failed = signal(false);
    public readonly hasMore = signal(true);

    public constructor() {
        this.destroyRef.onDestroy(() => this.request?.unsubscribe());
    }
    public seed(page: ShoppingListPage): void {
        this.request?.unsubscribe();
        this.query = '';
        this.page = page.nextPage ?? 1;
        this.lists.set(page.items.map(row => ({ ...row, completed: row.itemsCount > 0 && row.remainingCount === 0 })));
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
        this.failed.set(false);
    }
    public cancelSearch(): void {
        this.request?.unsubscribe();
        this.lists.set([]);
        this.loading.set(true);
        this.failed.set(false);
    }
    public reset(query: string): void {
        this.request?.unsubscribe();
        this.query = query;
        this.page = 1;
        this.lists.set([]);
        this.hasMore.set(true);
        this.loading.set(false);
        this.loadMore();
    }
    public loadMore(): void {
        if (this.loading() || !this.hasMore()) {
            return;
        }
        this.loading.set(true);
        this.failed.set(false);
        this.request = this.service.getPage(this.page, this.query).subscribe({
            next: rows => {
                const current = this.lists();
                const ids = new Set(current.map(row => row.id));
                this.lists.set(
                    [...current, ...rows.filter(row => !ids.has(row.id))].map(row => ({
                        ...row,
                        completed: row.itemsCount > 0 && row.remainingCount === 0,
                    })),
                );
                this.page += 1;
                this.hasMore.set(rows.length === PAGE_SIZE);
                this.loading.set(false);
            },
            error: () => {
                this.failed.set(true);
                this.loading.set(false);
            },
        });
    }
}
