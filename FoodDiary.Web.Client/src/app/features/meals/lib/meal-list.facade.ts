import type { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import { toLocalDayEndIso, toLocalDayStartIso } from '../../../shared/lib/local-date.utils';
import { PagedData } from '../../../shared/lib/paged-data.data';
import { FavoriteMealService } from '../api/favorite-meal.service';
import { MealService } from '../api/meal.service';
import type { FavoriteMeal, Meal, MealFilters } from '../models/meal.data';

@Injectable()
export class MealListFacade {
    private readonly mealService = inject(MealService);
    private readonly favoriteMealService = inject(FavoriteMealService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);

    public readonly pageSize = 10;
    public readonly consumptionData = new PagedData<Meal>();
    public readonly currentPageIndex = signal(0);
    public readonly errorKey = signal<string | null>(null);
    public readonly favorites = signal<FavoriteMeal[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesLoadingMore = signal(false);

    public loadFavorites(): void {
        this.isFavoritesLoadingMore.set(true);
        this.favoriteMealService
            .getAll()
            .pipe(
                catchError(() => {
                    this.showOperationError();
                    return of([]);
                }),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoritesLoadingMore.set(false);
                }),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
            });
    }

    public loadConsumptions(page: number, dateRange: FdUiDateRangeValue | null): Observable<void> {
        this.consumptionData.setLoading(true);
        const filters = this.buildFilters(dateRange);

        return this.mealService.query(page, this.pageSize, filters).pipe(
            tap(pageData => {
                this.consumptionData.setData(pageData);
                this.currentPageIndex.set(pageData.page - 1);
                this.clearError();
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.consumptionData.clearData();
                this.showLoadError();
                return of(void 0);
            }),
            finalize(() => {
                this.consumptionData.setLoading(false);
            }),
        );
    }

    public loadInitialOverview(dateRange: FdUiDateRangeValue | null): Observable<void> {
        this.consumptionData.setLoading(true);
        const filters = this.buildFilters(dateRange);

        return this.mealService.queryOverview(1, this.pageSize, filters, 10).pipe(
            tap(data => {
                this.consumptionData.setData(data.allConsumptions);
                this.favorites.set(data.favoriteItems);
                this.favoriteTotalCount.set(data.favoriteTotalCount);
                this.currentPageIndex.set(data.allConsumptions.page - 1);
                this.clearError();
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.consumptionData.clearData();
                this.favorites.set([]);
                this.favoriteTotalCount.set(0);
                this.showLoadError();
                return of(void 0);
            }),
            finalize(() => {
                this.consumptionData.setLoading(false);
            }),
        );
    }

    public repeatMeal(mealId: string, targetDate: string, mealType: string, dateRange: FdUiDateRangeValue | null): Observable<boolean> {
        return this.mealService.repeat(mealId, targetDate, mealType).pipe(
            switchMap(() => this.loadConsumptions(this.currentPageIndex() + 1, dateRange)),
            map(() => true),
            catchError(() => {
                this.showOperationError();
                return of(false);
            }),
        );
    }

    public deleteMeal(mealId: string, dateRange: FdUiDateRangeValue | null): Observable<boolean> {
        return this.mealService.deleteById(mealId).pipe(
            switchMap(() => this.loadConsumptions(this.currentPageIndex() + 1, dateRange)),
            map(() => true),
            catchError(() => {
                this.showOperationError();
                return of(false);
            }),
        );
    }

    public removeFavorite(favorite: FavoriteMeal): void {
        this.favoriteMealService
            .remove(favorite.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.favorites.update(list => list.filter(item => item.id !== favorite.id));
                    this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                    this.syncMealFavoriteState(favorite.mealId, false, null);
                },
                error: () => {
                    this.showOperationError();
                },
            });
    }

    public syncMealFavoriteState(mealId: string, isFavorite: boolean, favoriteMealId: string | null): void {
        this.consumptionData.items.update(items =>
            items.map(item => (item.id === mealId ? { ...item, isFavorite, favoriteMealId } : item)),
        );
    }

    private buildFilters(dateRange: FdUiDateRangeValue | null): MealFilters {
        return {
            dateFrom: toLocalDayStartIso(dateRange?.start ?? null),
            dateTo: toLocalDayEndIso(dateRange?.end ?? null),
        };
    }

    private clearError(): void {
        this.errorKey.set(null);
    }

    private showLoadError(): void {
        this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
    }

    private showOperationError(): void {
        this.toastService.error(this.translateService.instant('CONSUMPTION_LIST.OPERATION_ERROR_MESSAGE'));
    }
}
