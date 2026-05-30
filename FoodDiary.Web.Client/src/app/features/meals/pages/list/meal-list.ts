import { ChangeDetectionStrategy, Component, computed, DestroyRef, type ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { debounceTime, EMPTY, type Observable, switchMap } from 'rxjs';

import { AiInputActionBarComponent } from '../../../../components/shared/ai-input-bar/ai-input-action-bar';
import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { APP_FILTER_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { ViewportService } from '../../../../services/viewport.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { formatDateInputValue, getDateTimestamp, normalizeStartOfLocalDay } from '../../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import { resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import type { MealDetailComponent } from '../../components/detail/meal-detail/meal-detail';
import type { MealDetailActionResult } from '../../components/detail/meal-detail-lib/meal-detail.types';
import { AiMealCreateFacade } from '../../lib/ai/ai-meal-create.facade';
import { MealListFacade } from '../../lib/list/meal-list.facade';
import type { FavoriteMeal, Meal } from '../../models/meal.data';
import { MealListFiltersDialogComponent, type MealListFiltersDialogResult } from './meal-list-filters-dialog/meal-list-filters-dialog';
import type { FavoriteMealView, MealDateGroupView } from './meal-list-lib/meal-list.types';
import { MealListContentComponent, type MealListEmptyState } from './meal-list-sections/meal-list-content/meal-list-content';
import { MealListFavoritesComponent } from './meal-list-sections/meal-list-favorites/meal-list-favorites';

@Component({
    selector: 'fd-meal-list',
    templateUrl: './meal-list.html',
    styleUrls: ['./meal-list.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        AiInputActionBarComponent,
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
    private readonly aiMealCreateFacade = inject(AiMealCreateFacade);
    private readonly filterDebounceMs = inject(APP_FILTER_DEBOUNCE_MS);
    private readonly languageVersion = signal(0);

    protected searchForm: FormGroup<SearchFormGroup>;
    protected readonly consumptionData = this.mealListFacade.consumptionData;
    protected readonly errorKey = this.mealListFacade.errorKey;
    protected readonly favorites = this.mealListFacade.favorites;
    protected readonly favoriteViews = computed<FavoriteMealView[]>(() =>
        this.favorites().map(favorite => ({
            favorite,
            displayName: favorite.name,
            displayNameKey: `MEAL_TYPES.${favorite.mealType}`,
        })),
    );
    protected readonly favoriteTotalCount = this.mealListFacade.favoriteTotalCount;
    protected readonly isFavoritesLoadingMore = this.mealListFacade.isFavoritesLoadingMore;
    protected readonly favoriteLoadingIds = this.mealListFacade.favoriteLoadingIds;
    protected readonly isAiMealSaving = this.aiMealCreateFacade.isSaving;
    protected readonly aiMealClearToken = this.aiMealCreateFacade.clearToken;
    protected readonly groupedConsumptions = computed(() => {
        this.languageVersion();
        return this.groupByDate(this.consumptionData.items());
    });
    protected readonly isFavoritesOpen = signal(false);
    protected readonly isMobileView = this.viewportService.isMobile;
    protected readonly hasDateFilter = computed(() => {
        const dateRange = this.searchForm.controls.dateRange.value;
        return (dateRange?.start !== null && dateRange?.start !== undefined) || (dateRange?.end !== null && dateRange?.end !== undefined);
    });
    protected readonly emptyState = computed<MealListEmptyState | null>(() => {
        if (this.consumptionData.items().length > 0) {
            return null;
        }

        return this.hasDateFilter() ? 'no-results' : 'empty';
    });
    protected readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);
    protected readonly currentPageIndex = computed(() => this.mealListFacade.currentPageIndex());
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
                debounceTime(this.filterDebounceMs),
                switchMap(() => this.loadConsumptions(1)),
            )
            .subscribe();
    }

    protected loadFavorites(): void {
        this.mealListFacade.loadFavorites();
    }

    protected toggleFavorites(): void {
        this.isFavoritesOpen.update(v => !v);
    }

    protected repeatFavorite(favorite: FavoriteMeal): void {
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

    protected removeFavorite(favorite: FavoriteMeal): void {
        this.mealListFacade.removeFavorite(favorite);
    }

    protected onMealFavoriteToggle(meal: Meal): void {
        this.mealListFacade.toggleMealFavorite(meal);
    }

    protected onMealCreated(): void {
        this.scrollToTop();
        this.reloadCurrentPage();
    }

    protected onAiMealCreateRequested(result: AiInputBarResult): void {
        this.aiMealCreateFacade
            .createFromAiResult(result)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(meal => {
                if (meal !== null) {
                    this.onMealCreated();
                }
            });
    }

    protected loadConsumptions(page: number): Observable<void> {
        return this.mealListFacade.loadConsumptions(page, this.dateRange);
    }

    protected loadInitialOverview(): Observable<void> {
        return this.mealListFacade.loadInitialOverview(this.dateRange);
    }

    protected onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.loadConsumptions(pageIndex + 1)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    protected retryLoad(): void {
        const request = this.hasDateFilter() ? this.loadConsumptions(1) : this.loadInitialOverview();
        request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }

    protected async openMealDetailsAsync(consumption: Meal): Promise<void> {
        const { MealDetailComponent } = await import('../../components/detail/meal-detail/meal-detail');

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

    protected async goToMealAddAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionAddAsync();
    }

    protected openFilters(): void {
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
        return getDateTimestamp(value?.[key]);
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
            const key = formatDateInputValue(date);
            if (!buckets.has(key)) {
                const groupDate = normalizeStartOfLocalDay(date);
                buckets.set(key, { date: groupDate, dateLabel: this.formatGroupDate(groupDate), items: [] });
            }
            buckets.get(key)?.items.push(item);
        }

        return Array.from(buckets.values()).sort((a, b) => b.date.getTime() - a.date.getTime());
    }

    private formatGroupDate(date: Date): string {
        return new Intl.DateTimeFormat(resolveAppLocale(this.translateService.getCurrentLang()), {
            day: 'numeric',
            month: 'long',
            year: 'numeric',
        }).format(date);
    }
}

type SearchFormValues = {
    dateRange: FdUiDateRangeValue | null;
};

type SearchFormGroup = FormGroupControls<SearchFormValues>;
