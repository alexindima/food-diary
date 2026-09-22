import { DestroyRef, inject, Injectable, signal } from '@angular/core';
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

    public reloadAfterRemoval(): void {
        const lastPage = Math.max(1, Math.ceil((this.total() - 1) / this.pageSize));
        this.load(Math.min(this.page(), lastPage));
    }

    public load(page = 1, search = this.search()): void {
        this.search.set(search.trim());
        this.page.set(page);
        this.requests.next({ page, search: this.search() });
    }
}
