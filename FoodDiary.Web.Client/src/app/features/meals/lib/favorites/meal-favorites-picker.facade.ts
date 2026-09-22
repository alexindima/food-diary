import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, finalize, Subject, switchMap, tap } from 'rxjs';

import { FavoriteMealService } from '../../api/favorite-meal.service';
import type { FavoriteMeal } from '../../models/meal.data';

@Injectable()
export class MealFavoritesPickerFacade {
    private readonly api = inject(FavoriteMealService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly requests = new Subject<{ page: number; search: string }>();
    public readonly items = signal<FavoriteMeal[]>([]);
    public readonly loading = signal(false);
    public readonly failed = signal(false);
    public readonly page = signal(1);
    public readonly total = signal(0);
    public readonly search = signal('');
    public readonly pageSize = 10;
    public readonly revision = signal(0);
    public readonly removedIds = signal<ReadonlySet<string>>(new Set());
    public readonly paginationTotal = computed(() => this.total() + this.removedIds().size);

    public constructor() {
        this.requests
            .pipe(
                switchMap(request => {
                    this.loading.set(true);
                    this.failed.set(false);
                    this.items.set([]);
                    return this.api.getPage(request.page, this.pageSize, request.search).pipe(
                        tap(result => {
                            this.items.set(result.data);
                            this.total.set(result.totalItems);
                        }),
                        catchError(() => {
                            this.failed.set(true);
                            return EMPTY;
                        }),
                        finalize(() => {
                            this.loading.set(false);
                        }),
                    );
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    public markRemoved(id: string): void {
        if (this.removedIds().has(id) || !this.items().some(item => item.id === id)) {
            return;
        }
        this.removedIds.update(ids => new Set([...ids, id]));
        this.total.update(total => Math.max(0, total - 1));
    }

    public markRestored(id: string): void {
        if (!this.removedIds().has(id)) {
            return;
        }
        this.removedIds.update(ids => new Set([...ids].filter(value => value !== id)));
        this.total.update(total => total + 1);
    }

    public load(page = 1, search = this.search()): void {
        if (search.trim() === this.search() && this.removedIds().size > 0) {
            page = Math.min(page, Math.max(1, Math.ceil(this.total() / this.pageSize)));
        }
        this.revision.update(value => value + 1);
        this.removedIds.set(new Set());
        this.search.set(search.trim());
        this.page.set(page);
        this.requests.next({ page, search: this.search() });
    }
}
