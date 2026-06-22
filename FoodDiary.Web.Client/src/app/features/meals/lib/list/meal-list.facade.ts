import { DestroyRef, inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import { toLocalDayEndIso, toLocalDayStartIso } from '../../../../shared/lib/local-date.utils';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealService } from '../../api/meal.service';
import type { FavoriteMeal, Meal, MealFilters } from '../../models/meal.data';
import { MEAL_LIST_OVERVIEW_FAVORITES_LIMIT, MEAL_LIST_PAGE_SIZE } from './meal-list.config';

export type MealListStructuredFilters = {
    dateRange: FdUiDateRangeValue | null;
    mealTypes: string[];
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
    hasAiSession: boolean | null;
};

@Service()
export class MealListFacade {
    private readonly mealService = inject(MealService);
    private readonly favoriteMealService = inject(FavoriteMealService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);

    public readonly pageSize = MEAL_LIST_PAGE_SIZE;
    public readonly consumptionData = new PagedData<Meal>();
    public readonly currentPageIndex = signal(0);
    public readonly errorKey = signal<string | null>(null);
    public readonly favorites = signal<FavoriteMeal[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());

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

    public loadConsumptions(page: number, filtersModel: MealListStructuredFilters): Observable<void> {
        this.consumptionData.setLoading(true);
        const filters = this.buildFilters(filtersModel);

        return this.mealService.query(page, this.pageSize, filters).pipe(
            tap(pageData => {
                this.consumptionData.setData(pageData);
                this.currentPageIndex.set(pageData.page - 1);
                this.clearError();
            }),
            map(() => void 0),
            catchError((_error: unknown) => {
                this.consumptionData.clearData();
                this.showLoadError();
                return of(void 0);
            }),
            finalize(() => {
                this.consumptionData.setLoading(false);
            }),
        );
    }

    public loadInitialOverview(filtersModel: MealListStructuredFilters): Observable<void> {
        this.consumptionData.setLoading(true);
        const filters = this.buildFilters(filtersModel);

        return this.mealService.queryOverview(1, this.pageSize, filters, MEAL_LIST_OVERVIEW_FAVORITES_LIMIT).pipe(
            tap(data => {
                this.consumptionData.setData(data.allConsumptions);
                this.favorites.set(data.favoriteItems);
                this.favoriteTotalCount.set(data.favoriteTotalCount);
                this.currentPageIndex.set(data.allConsumptions.page - 1);
                this.clearError();
            }),
            map(() => void 0),
            catchError((_error: unknown) => {
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

    public repeatMeal(mealId: string, targetDate: string, mealType: string, filtersModel: MealListStructuredFilters): Observable<boolean> {
        return this.mealService.repeat(mealId, targetDate, mealType).pipe(
            switchMap(() => this.loadConsumptions(this.currentPageIndex() + 1, filtersModel)),
            map(() => true),
            catchError(() => {
                this.showOperationError();
                return of(false);
            }),
        );
    }

    public deleteMeal(mealId: string, filtersModel: MealListStructuredFilters): Observable<boolean> {
        return this.mealService.deleteById(mealId).pipe(
            switchMap(() => this.loadConsumptions(this.currentPageIndex() + 1, filtersModel)),
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

    public toggleMealFavorite(meal: Meal): void {
        if (this.favoriteLoadingIds().has(meal.id)) {
            return;
        }

        this.setFavoriteLoading(meal.id, true);

        if (meal.isFavorite === true) {
            this.removeMealFavorite(meal);
            return;
        }

        this.favoriteMealService
            .add(meal.id)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(meal.id, false);
                }),
            )
            .subscribe({
                next: favorite => {
                    this.syncMealFavoriteState(meal.id, true, favorite.id);
                    this.loadFavorites();
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

    private removeMealFavorite(meal: Meal): void {
        const favoriteId = meal.favoriteMealId;
        const request$ =
            favoriteId !== null && favoriteId !== undefined && favoriteId.length > 0
                ? this.favoriteMealService.remove(favoriteId)
                : this.favoriteMealService.getAll().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.mealId === meal.id);
                          return match === undefined ? of(null) : this.favoriteMealService.remove(match.id);
                      }),
                  );

        request$
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(meal.id, false);
                }),
            )
            .subscribe({
                next: () => {
                    this.syncMealFavoriteState(meal.id, false, null);
                    this.loadFavorites();
                },
                error: () => {
                    this.showOperationError();
                },
            });
    }

    private setFavoriteLoading(mealId: string, isLoading: boolean): void {
        this.favoriteLoadingIds.update(current => {
            const next = new Set(current);
            if (isLoading) {
                next.add(mealId);
            } else {
                next.delete(mealId);
            }

            return next;
        });
    }

    private buildFilters(filtersModel: MealListStructuredFilters): MealFilters {
        const filters: MealFilters = {
            dateFrom: toLocalDayStartIso(filtersModel.dateRange?.start ?? null),
            dateTo: toLocalDayEndIso(filtersModel.dateRange?.end ?? null),
        };

        if (filtersModel.mealTypes.length > 0) {
            filters.mealTypes = filtersModel.mealTypes.join(',');
        }
        if (filtersModel.caloriesFrom !== null) {
            filters.caloriesFrom = filtersModel.caloriesFrom;
        }
        if (filtersModel.caloriesTo !== null) {
            filters.caloriesTo = filtersModel.caloriesTo;
        }
        if (filtersModel.hasImage !== null) {
            filters.hasImage = filtersModel.hasImage;
        }
        if (filtersModel.hasAiSession !== null) {
            filters.hasAiSession = filtersModel.hasAiSession;
        }

        return filters;
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
