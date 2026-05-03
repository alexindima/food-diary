import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { catchError, debounceTime, finalize, map, Observable, of, switchMap } from 'rxjs';

import { AiInputBarComponent } from '../../../../components/shared/ai-input-bar/ai-input-bar.component';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import { MealCardComponent } from '../../../../components/shared/meal-card/meal-card.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { LocalizedDatePipe } from '../../../../pipes/localized-date.pipe';
import { NavigationService } from '../../../../services/navigation.service';
import { ViewportService } from '../../../../services/viewport.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealService } from '../../api/meal.service';
import type { MealDetailActionResult, MealDetailComponent } from '../../components/detail/meal-detail.component';
import { FavoriteMeal, Meal, MealFilters } from '../../models/meal.data';
import { MealListFiltersDialogComponent, MealListFiltersDialogResult } from './meal-list-filters-dialog.component';

@Component({
    selector: 'fd-meal-list',
    templateUrl: './meal-list.component.html',
    styleUrls: ['./meal-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        DecimalPipe,
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FdUiIconComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        MealCardComponent,
        AiInputBarComponent,
        FavoritesSectionComponent,
        LocalizedDatePipe,
    ],
})
export class MealListComponent {
    private readonly mealService = inject(MealService);
    private readonly favoriteMealService = inject(FavoriteMealService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly viewportService = inject(ViewportService);

    public searchForm: FormGroup<SearchFormGroup>;
    public consumptionData: PagedData<Meal> = new PagedData<Meal>();
    public currentPageIndex = 0;
    public readonly groupedConsumptions = computed(() => this.groupByDate(this.consumptionData.items()));
    public readonly errorKey = signal<string | null>(null);
    public readonly favorites = signal<FavoriteMeal[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesOpen = signal(false);
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly hasDateFilter = computed(() => {
        const dateRange = this.searchForm.controls.dateRange.value;
        return !!dateRange?.start || !!dateRange?.end;
    });
    public readonly isEmptyState = computed(() => this.consumptionData.items().length === 0 && !this.hasDateFilter());
    public readonly isNoResultsState = computed(() => this.consumptionData.items().length === 0 && this.hasDateFilter());
    public readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);
    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.searchForm = new FormGroup<SearchFormGroup>({
            dateRange: new FormControl<FdUiDateRangeValue | null>(null),
        });

        this.loadInitialOverview().subscribe();

        this.searchForm.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                debounceTime(300),
                switchMap(() => this.loadConsumptions(1)),
            )
            .subscribe();
    }

    public loadFavorites(): void {
        this.isFavoritesLoadingMore.set(true);
        this.favoriteMealService
            .getAll()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => this.isFavoritesLoadingMore.set(false)),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
            });
    }

    public toggleFavorites(): void {
        this.isFavoritesOpen.update(v => !v);
    }

    public repeatFavorite(favorite: FavoriteMeal): void {
        const today = new Date().toISOString().slice(0, 10);
        this.mealService.repeat(favorite.mealId, today).subscribe({
            next: () => {
                this.scrollToTop();
                this.loadConsumptions(this.currentPageIndex + 1).subscribe();
            },
        });
    }

    public removeFavorite(favorite: FavoriteMeal): void {
        this.favoriteMealService.remove(favorite.id).subscribe({
            next: () => this.favorites.update(list => list.filter(f => f.id !== favorite.id)),
        });
    }

    public onMealCreated(): void {
        this.scrollToTop();
        this.reloadCurrentPage();
    }

    public loadConsumptions(page: number): Observable<void> {
        this.consumptionData.setLoading(true);
        const dateRange = this.searchForm.controls.dateRange.value;

        const filters: MealFilters = {
            dateFrom: this.toIsoDate(dateRange?.start ?? null),
            dateTo: this.toIsoDate(dateRange?.end ?? null),
        };

        return this.mealService.query(page, 10, filters).pipe(
            map(pageData => {
                this.consumptionData.setData(pageData);
                this.currentPageIndex = pageData.page - 1;
                this.consumptionData.setLoading(false);
                this.errorKey.set(null);
            }),
            catchError(() => {
                this.consumptionData.clearData();
                this.consumptionData.setLoading(false);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of();
            }),
        );
    }

    public loadInitialOverview(): Observable<void> {
        this.consumptionData.setLoading(true);
        const dateRange = this.searchForm.controls.dateRange.value;
        const filters: MealFilters = {
            dateFrom: this.toIsoDate(dateRange?.start ?? null),
            dateTo: this.toIsoDate(dateRange?.end ?? null),
        };

        return this.mealService.queryOverview(1, 10, filters, 10).pipe(
            map(data => {
                this.consumptionData.setData(data.allConsumptions);
                this.favorites.set(data.favoriteItems);
                this.favoriteTotalCount.set(data.favoriteTotalCount);
                this.currentPageIndex = data.allConsumptions.page - 1;
                this.consumptionData.setLoading(false);
                this.errorKey.set(null);
            }),
            catchError(() => {
                this.consumptionData.clearData();
                this.favorites.set([]);
                this.favoriteTotalCount.set(0);
                this.consumptionData.setLoading(false);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of();
            }),
        );
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex = pageIndex;
        this.loadConsumptions(pageIndex + 1).subscribe();
    }

    public async openMealDetails(consumption: Meal): Promise<void> {
        const { MealDetailComponent } = await import('../../components/detail/meal-detail.component');

        this.fdDialogService
            .open<MealDetailComponent, Meal, MealDetailActionResult>(MealDetailComponent, {
                preset: 'detail',
                data: consumption,
            })
            .afterClosed()
            .subscribe(data => {
                if (!data) {
                    return;
                }

                if (data.action === 'FavoriteChanged') {
                    this.loadFavorites();
                    this.reloadCurrentPage();
                } else if (data.action === 'Edit') {
                    void this.navigationService.navigateToConsumptionEdit(data.id);
                } else if (data.action === 'Repeat') {
                    const today = new Date().toISOString().slice(0, 10);
                    this.mealService.repeat(data.id, today).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.reloadCurrentPage();
                        },
                    });
                } else if (data.action === 'Delete') {
                    this.mealService.deleteById(data.id).subscribe({
                        next: () => {
                            this.scrollToTop();
                            this.reloadCurrentPage();
                        },
                    });
                }
            });
    }

    public async goToMealAdd(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public openFilters(): void {
        const currentDateRange = this.searchForm.controls.dateRange.value;

        this.fdDialogService
            .open<MealListFiltersDialogComponent, { dateRange: FdUiDateRangeValue | null }, MealListFiltersDialogResult | null>(
                MealListFiltersDialogComponent,
                {
                    preset: 'form',
                    data: {
                        dateRange: currentDateRange,
                    },
                },
            )
            .afterClosed()
            .subscribe(result => {
                if (!result) {
                    return;
                }

                const nextDateRange = result.dateRange ?? null;
                const currentStart = currentDateRange?.start?.getTime() ?? null;
                const currentEnd = currentDateRange?.end?.getTime() ?? null;
                const nextStart = nextDateRange?.start?.getTime() ?? null;
                const nextEnd = nextDateRange?.end?.getTime() ?? null;

                if (currentStart === nextStart && currentEnd === nextEnd) {
                    return;
                }

                this.searchForm.controls.dateRange.setValue(nextDateRange);
            });
    }

    protected scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected reloadCurrentPage(): void {
        this.loadConsumptions(this.currentPageIndex + 1).subscribe();
    }

    private toIsoDate(date: Date | null | undefined): string | undefined {
        if (!date) {
            return undefined;
        }

        const normalized = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
        return normalized.toISOString();
    }

    private groupByDate(items: Meal[]): { date: Date; items: Meal[] }[] {
        const buckets = new Map<string, { date: Date; items: Meal[] }>();

        for (const item of items) {
            const date = new Date(item.date);
            const key = date.toISOString().slice(0, 10);
            if (!buckets.has(key)) {
                buckets.set(key, { date, items: [] });
            }
            buckets.get(key)!.items.push(item);
        }

        return Array.from(buckets.values()).sort((a, b) => b.date.getTime() - a.date.getTime());
    }
}

interface SearchFormValues {
    dateRange: FdUiDateRangeValue | null;
}

type SearchFormGroup = FormGroupControls<SearchFormValues>;
