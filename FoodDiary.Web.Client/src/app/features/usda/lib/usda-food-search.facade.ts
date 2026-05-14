import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, type Observable, of, switchMap } from 'rxjs';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../config/runtime-ui.tokens';
import { UsdaService } from '../api/usda.service';
import type { UsdaFood } from '../models/usda.data';

const USDA_SEARCH_MIN_LENGTH = 2;

@Injectable({ providedIn: 'root' })
export class UsdaFoodSearchFacade {
    private readonly usdaService = inject(UsdaService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);

    public readonly searchQuery = signal('');
    public readonly results = signal<UsdaFood[]>([]);
    public readonly isLoading = signal(false);
    public readonly selectedFood = signal<UsdaFood | null>(null);

    public constructor() {
        toObservable(this.searchQuery)
            .pipe(
                debounceTime(this.searchDebounceMs),
                distinctUntilChanged(),
                switchMap(query => this.searchFoods(query)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(foods => {
                this.results.set(foods);
                this.isLoading.set(false);
            });
    }

    public reset(): void {
        this.searchQuery.set('');
        this.results.set([]);
        this.isLoading.set(false);
        this.selectedFood.set(null);
    }

    public updateSearchQuery(value: string): void {
        this.searchQuery.set(value);
        this.selectedFood.set(null);
    }

    public selectFood(food: UsdaFood): void {
        this.selectedFood.set(food);
    }

    private searchFoods(query: string): Observable<UsdaFood[]> {
        if (query.length < USDA_SEARCH_MIN_LENGTH) {
            this.results.set([]);
            this.isLoading.set(false);
            return of<UsdaFood[]>([]);
        }

        this.isLoading.set(true);
        return this.usdaService.searchFoods(query);
    }
}
