import { computed, inject, Injectable, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import type { ShoppingListPage, ShoppingListSummary } from '../../../shared/models/shopping-list.data';
import { ShoppingListService } from '../api/shopping-list.service';

const PAGE_SIZE = 20;
@Injectable()
export class ShoppingListBrowserFacade {
    private readonly service = inject(ShoppingListService);
    private readonly requestedPage = signal<{ page: number; query: string } | undefined>(undefined);
    private readonly debouncing = signal(false);
    private readonly pageResource = rxResource({
        params: this.requestedPage,
        stream: ({ params, abortSignal }) =>
            this.service.getPage(params.page, params.query).pipe(
                map(rows => {
                    if (!abortSignal.aborted && this.requestedPage() === params) {
                        this.appendRows(rows);
                    }
                }),
            ),
    });

    private page = 1;
    private query = '';
    public readonly lists = signal<ShoppingListSummary[]>([]);
    public readonly loading = computed(() => this.debouncing() || this.pageResource.isLoading());
    public readonly failed = computed(() => this.pageResource.error() !== undefined);
    public readonly hasMore = signal(true);

    public seed(page: ShoppingListPage): void {
        this.cancelPageRequest();
        this.query = '';
        this.page = page.nextPage ?? 1;
        this.lists.set(page.items.map(row => ({ ...row, completed: row.itemsCount > 0 && row.remainingCount === 0 })));
        this.hasMore.set(page.hasMore);
        this.debouncing.set(false);
    }

    public cancelSearch(): void {
        this.cancelPageRequest();
        this.lists.set([]);
        this.debouncing.set(true);
    }

    public reset(query: string): void {
        this.cancelPageRequest();
        this.query = query;
        this.page = 1;
        this.lists.set([]);
        this.hasMore.set(true);
        this.debouncing.set(false);
        this.loadMore();
    }

    public loadMore(): void {
        if (this.loading() || !this.hasMore()) {
            return;
        }

        this.requestedPage.set({ page: this.page, query: this.query });
    }

    private cancelPageRequest(): void {
        this.requestedPage.set(undefined);
        // Abort immediately, before the effect handles the cleared request.
        this.pageResource.set(undefined);
    }

    private appendRows(rows: ShoppingListSummary[]): void {
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
    }
}
