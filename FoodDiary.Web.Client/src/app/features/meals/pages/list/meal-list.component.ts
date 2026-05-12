import { ChangeDetectionStrategy, Component, computed, DestroyRef, type ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { debounceTime, EMPTY, type Observable, switchMap } from 'rxjs';

import { AiInputBarComponent } from '../../../../components/shared/ai-input-bar/ai-input-bar.component';
import type { MealFavoriteChange } from '../../../../components/shared/meal-card/meal-card.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { ViewportService } from '../../../../services/viewport.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import type { MealDetailActionResult, MealDetailComponent } from '../../components/detail/meal-detail.component';
import { MealListFacade } from '../../lib/meal-list.facade';
import type { FavoriteMeal, Meal } from '../../models/meal.data';
import { MealListContentComponent } from './meal-list-content.component';
import { MealListFavoritesComponent } from './meal-list-favorites.component';
import { MealListFiltersDialogComponent, type MealListFiltersDialogResult } from './meal-list-filters-dialog.component';

const FILTER_CHANGE_DEBOUNCE_MS = 300;

export interface FavoriteMealView {
    favorite: FavoriteMeal;
    displayName: string | null;
    displayNameKey: string;
}

@Component({
    selector: 'fd-meal-list',
    templateUrl: './meal-list.component.html',
    styleUrls: ['./meal-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        AiInputBarComponent,
        MealListContentComponent,
        MealListFavoritesComponent,
    ],
    providers: [MealListFacade],
})
export class MealListComponent {
    private readonly mealListFacade = inject(MealListFacade);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly viewportService = inject(ViewportService);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public searchForm: FormGroup<SearchFormGroup>;
    public readonly consumptionData = this.mealListFacade.consumptionData;
    public readonly errorKey = this.mealListFacade.errorKey;
    public readonly favorites = this.mealListFacade.favorites;
    public readonly favoriteViews = computed<FavoriteMealView[]>(() =>
        this.favorites().map(favorite => ({
            favorite,
            displayName: favorite.name,
            displayNameKey: `MEAL_TYPES.${favorite.mealType}`,
        })),
    );
    public readonly favoriteTotalCount = this.mealListFacade.favoriteTotalCount;
    public readonly isFavoritesLoadingMore = this.mealListFacade.isFavoritesLoadingMore;
    public readonly groupedConsumptions = computed(() => {
        this.languageVersion();
        return this.groupByDate(this.consumptionData.items());
    });
    public readonly isFavoritesOpen = signal(false);
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly hasDateFilter = computed(() => {
        const dateRange = this.searchForm.controls.dateRange.value;
        return (dateRange?.start !== null && dateRange?.start !== undefined) || (dateRange?.end !== null && dateRange?.end !== undefined);
    });
    public readonly isEmptyState = computed(() => this.consumptionData.items().length === 0 && !this.hasDateFilter());
    public readonly isNoResultsState = computed(() => this.consumptionData.items().length === 0 && this.hasDateFilter());
    public readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);
    public readonly currentPageIndex = computed(() => this.mealListFacade.currentPageIndex());
    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.searchForm = new FormGroup<SearchFormGroup>({
            dateRange: new FormControl<FdUiDateRangeValue | null>(null),
        });

        this.loadInitialOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });

        this.searchForm.valueChanges
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                debounceTime(FILTER_CHANGE_DEBOUNCE_MS),
                switchMap(() => this.loadConsumptions(1)),
            )
            .subscribe();
    }

    public loadFavorites(): void {
        this.mealListFacade.loadFavorites();
    }

    public toggleFavorites(): void {
        this.isFavoritesOpen.update(v => !v);
    }

    public repeatFavorite(favorite: FavoriteMeal): void {
        const targetDate = new Date();
        this.mealListFacade
            .repeatMeal(favorite.mealId, targetDate.toISOString(), resolveMealTypeByTime(targetDate), this.dateRange)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(repeated => {
                if (repeated) {
                    this.scrollToTop();
                }
            });
    }

    public removeFavorite(favorite: FavoriteMeal): void {
        this.mealListFacade.removeFavorite(favorite);
    }

    public onMealFavoriteChanged(meal: Meal, change: MealFavoriteChange): void {
        this.mealListFacade.syncMealFavoriteState(meal.id, change.isFavorite, change.favoriteMealId);
        this.loadFavorites();
    }

    public onMealCreated(): void {
        this.scrollToTop();
        this.reloadCurrentPage();
    }

    public loadConsumptions(page: number): Observable<void> {
        return this.mealListFacade.loadConsumptions(page, this.dateRange);
    }

    public loadInitialOverview(): Observable<void> {
        return this.mealListFacade.loadInitialOverview(this.dateRange);
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.loadConsumptions(pageIndex + 1)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    public retryLoad(): void {
        const request = this.hasDateFilter() ? this.loadConsumptions(1) : this.loadInitialOverview();
        request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }

    public async openMealDetailsAsync(consumption: Meal): Promise<void> {
        const { MealDetailComponent } = await import('../../components/detail/meal-detail.component');

        this.fdDialogService
            .open<MealDetailComponent, Meal, MealDetailActionResult>(MealDetailComponent, {
                preset: 'detail',
                data: consumption,
            })
            .afterClosed()
            .pipe(
                switchMap(data => {
                    if (data === undefined) {
                        return EMPTY;
                    }

                    if (data.action === 'FavoriteChanged') {
                        this.loadFavorites();
                        this.reloadCurrentPage();
                        return EMPTY;
                    }

                    if (data.action === 'Edit') {
                        void this.navigationService.navigateToConsumptionEditAsync(data.id);
                        return EMPTY;
                    }

                    if (data.action === 'Repeat') {
                        const targetDate = new Date();
                        return this.mealListFacade.repeatMeal(
                            data.id,
                            targetDate.toISOString(),
                            resolveMealTypeByTime(targetDate),
                            this.dateRange,
                        );
                    }

                    return this.mealListFacade.deleteMeal(data.id, this.dateRange);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(changed => {
                if (changed) {
                    this.scrollToTop();
                }
            });
    }

    public async goToMealAddAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionAddAsync();
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
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                this.applyFilterDialogResult(currentDateRange, result);
            });
    }

    private applyFilterDialogResult(
        currentDateRange: FdUiDateRangeValue | null,
        result: MealListFiltersDialogResult | null | undefined,
    ): void {
        if (result === null || result === undefined) {
            return;
        }

        const nextDateRange = result.dateRange ?? null;
        if (this.hasSameDateRange(currentDateRange, nextDateRange)) {
            return;
        }

        this.searchForm.controls.dateRange.setValue(nextDateRange);
    }

    private hasSameDateRange(left: FdUiDateRangeValue | null, right: FdUiDateRangeValue | null): boolean {
        return (
            this.getDateRangeTimestamp(left, 'start') === this.getDateRangeTimestamp(right, 'start') &&
            this.getDateRangeTimestamp(left, 'end') === this.getDateRangeTimestamp(right, 'end')
        );
    }

    private getDateRangeTimestamp(value: FdUiDateRangeValue | null, key: keyof FdUiDateRangeValue): number | null {
        return value?.[key]?.getTime() ?? null;
    }

    protected scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected reloadCurrentPage(): void {
        this.loadConsumptions(this.currentPageIndex() + 1)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    private get dateRange(): FdUiDateRangeValue | null {
        return this.searchForm.controls.dateRange.value;
    }

    private groupByDate(items: Meal[]): MealDateGroupView[] {
        const buckets = new Map<string, MealDateGroupView>();

        for (const item of items) {
            const date = new Date(item.date);
            const key = this.toLocalDateInputValue(date);
            if (!buckets.has(key)) {
                const groupDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
                buckets.set(key, { date: groupDate, dateLabel: this.formatGroupDate(groupDate), items: [] });
            }
            buckets.get(key)?.items.push(item);
        }

        return Array.from(buckets.values()).sort((a, b) => b.date.getTime() - a.date.getTime());
    }

    private formatGroupDate(date: Date): string {
        return new Intl.DateTimeFormat(this.translateService.getCurrentLang() === 'ru' ? 'ru-RU' : 'en-US', {
            day: 'numeric',
            month: 'long',
            year: 'numeric',
        }).format(date);
    }

    private toLocalDateInputValue(date: Date): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

interface SearchFormValues {
    dateRange: FdUiDateRangeValue | null;
}

type SearchFormGroup = FormGroupControls<SearchFormValues>;

export interface MealDateGroupView {
    date: Date;
    dateLabel: string;
    items: Meal[];
}
