import { ChangeDetectionStrategy, Component, computed, DestroyRef, type ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import type { FdUiDateRangeValue } from 'fd-ui-kit';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { Observable } from 'rxjs';

import { AiInputActionBarComponent } from '../../../../components/shared/ai-input-bar/ai-input-action-bar';
import type { AiInputBarResult } from '../../../../components/shared/ai-input-bar/ai-input-bar.types';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { NavigationService } from '../../../../services/navigation.service';
import { formatDateInputValue, getDateTimestamp, normalizeStartOfLocalDay } from '../../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import { normalizeMealType, resolveMealTypeByTime } from '../../../../shared/lib/meal-type.util';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { AiMealCreateFacade } from '../../lib/ai/ai-meal-create.facade';
import { MealListFacade, type MealListStructuredFilters } from '../../lib/list/meal-list.facade';
import type { FavoriteMeal, Meal } from '../../models/meal.data';
import { MealListFiltersDialogComponent, type MealListFiltersDialogResult } from './meal-list-filters-dialog/meal-list-filters-dialog';
import type { FavoriteMealView, MealDateGroupView } from './meal-list-lib/meal-list.types';
import { MealListContentComponent, type MealListEmptyState } from './meal-list-sections/meal-list-content/meal-list-content';
import { MealListFavoritesComponent } from './meal-list-sections/meal-list-favorites/meal-list-favorites';
import { MEAL_LIST_TOUR } from './meal-list-tour';

@Component({
    selector: 'fd-meal-list',
    templateUrl: './meal-list.html',
    styleUrls: ['./meal-list.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
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
    providers: [AiMealCreateFacade, MealListFacade],
})
export class MealListComponent {
    private readonly mealListFacade = inject(MealListFacade);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly viewportService = inject(ViewportService);
    private readonly translateService = inject(TranslateService);
    private readonly aiMealCreateFacade = inject(AiMealCreateFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly languageVersion = signal(0);

    protected readonly searchModel = signal<SearchFormValues>({
        dateRange: null,
        mealTypes: [],
        caloriesFrom: null,
        caloriesTo: null,
        hasImage: null,
        hasAiSession: null,
    });
    protected readonly consumptionData = this.mealListFacade.consumptionData;
    protected readonly errorKey = this.mealListFacade.errorKey;
    protected readonly favorites = this.mealListFacade.favorites;
    protected readonly favoriteViews = computed<FavoriteMealView[]>(() =>
        this.favorites().map(favorite => {
            const mealType = normalizeMealType(favorite.mealType);
            return {
                favorite,
                displayName: favorite.name,
                displayNameKey: mealType === null ? 'CONSUMPTION_LIST.FAVORITE_UNNAMED' : `MEAL_TYPES.${mealType}`,
            };
        }),
    );
    protected readonly favoriteTotalCount = this.mealListFacade.favoriteTotalCount;
    protected readonly isFavoritesLoadingMore = this.mealListFacade.isFavoritesLoadingMore;
    protected readonly favoriteLoadingIds = this.mealListFacade.favoriteLoadingIds;
    protected readonly isAiMealSaving = this.aiMealCreateFacade.isSaving;
    protected readonly aiMealClearToken = this.aiMealCreateFacade.clearToken;
    protected readonly plannedGroups = computed(() => {
        this.languageVersion();
        return this.groupByDate(this.plannedConsumptions(), 'asc');
    });
    protected readonly groupedConsumptions = computed(() => {
        this.languageVersion();
        return this.groupByDate(this.currentConsumptions(), 'desc');
    });
    protected readonly isFavoritesOpen = signal(false);
    protected readonly isPlannedOpen = signal(true);
    protected readonly isMobileView = this.viewportService.isMobile;
    protected readonly hasDateFilter = computed(() => {
        const dateRange = this.searchModel().dateRange;
        return (dateRange?.start !== null && dateRange?.start !== undefined) || (dateRange?.end !== null && dateRange?.end !== undefined);
    });
    protected readonly hasStructuredFilters = computed(
        () =>
            this.searchModel().mealTypes.length > 0 ||
            this.searchModel().caloriesFrom !== null ||
            this.searchModel().caloriesTo !== null ||
            this.searchModel().hasImage !== null ||
            this.searchModel().hasAiSession !== null,
    );
    protected readonly hasActiveFilters = computed(() => this.hasDateFilter() || this.hasStructuredFilters());
    protected readonly activeFilterKeys = computed(() => {
        const model = this.searchModel();
        const keys: string[] = [];
        if (model.mealTypes.length > 0) {
            keys.push('CONSUMPTION_LIST.FILTER_MEAL_TYPES_ACTIVE');
        }
        if (model.caloriesFrom !== null || model.caloriesTo !== null) {
            keys.push('CONSUMPTION_LIST.FILTER_CALORIES_ACTIVE');
        }
        if (model.hasImage === true) {
            keys.push('CONSUMPTION_LIST.FILTER_IMAGE_WITH');
        }
        if (model.hasImage === false) {
            keys.push('CONSUMPTION_LIST.FILTER_IMAGE_WITHOUT');
        }
        if (model.hasAiSession === true) {
            keys.push('CONSUMPTION_LIST.FILTER_AI_WITH');
        }
        if (model.hasAiSession === false) {
            keys.push('CONSUMPTION_LIST.FILTER_AI_WITHOUT');
        }

        return keys;
    });
    protected readonly activeDateFilterStart = computed(() => this.formatDateFilterValue(this.searchModel().dateRange?.start));
    protected readonly activeDateFilterEnd = computed(() => this.formatDateFilterValue(this.searchModel().dateRange?.end));
    protected readonly emptyState = computed<MealListEmptyState | null>(() => {
        if (this.consumptionData.items().length > 0) {
            return null;
        }

        return this.hasActiveFilters() ? 'no-results' : 'empty';
    });
    protected readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);
    protected readonly currentPageIndex = computed(() => this.mealListFacade.currentPageIndex());
    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public constructor() {
        this.loadInitialOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected loadFavorites(): void {
        this.mealListFacade.loadFavorites();
    }

    protected toggleFavorites(): void {
        this.isFavoritesOpen.update(v => !v);
    }

    protected togglePlanned(): void {
        this.isPlannedOpen.update(v => !v);
    }

    protected repeatFavorite(favorite: FavoriteMeal): void {
        const targetDate = new Date();
        this.mealListFacade
            .repeatMeal(favorite.mealId, targetDate.toISOString(), resolveMealTypeByTime(targetDate), this.structuredFilters)
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
        return this.mealListFacade.loadConsumptions(page, this.structuredFilters);
    }

    protected loadInitialOverview(): Observable<void> {
        return this.mealListFacade.loadInitialOverview(this.structuredFilters);
    }

    protected onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.loadConsumptions(pageIndex + 1)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    protected retryLoad(): void {
        const request = this.hasActiveFilters() ? this.loadConsumptions(1) : this.loadInitialOverview();
        request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }

    protected async openMealDetailsAsync(consumption: Meal): Promise<void> {
        if (await this.mealListFacade.handleMealDetailsAsync(consumption, this.structuredFilters)) {
            this.scrollToTop();
        }
    }

    protected async goToMealAddAsync(): Promise<void> {
        await this.navigationService.navigateToConsumptionAddAsync();
    }

    protected openFilters(): void {
        const currentDateRange = this.searchModel().dateRange;
        const currentFilters = this.structuredFilters;

        this.fdDialogService
            .open<MealListFiltersDialogComponent, MealListStructuredFilters, MealListFiltersDialogResult | null>(
                MealListFiltersDialogComponent,
                {
                    preset: 'form',
                    data: currentFilters,
                },
            )
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                this.applyFilterDialogResult(currentDateRange, currentFilters, result);
            });
    }

    protected startMealListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(MEAL_LIST_TOUR), { force });
    }

    private applyFilterDialogResult(
        currentDateRange: FdUiDateRangeValue | null,
        currentFilters: MealListStructuredFilters,
        result: MealListFiltersDialogResult | null | undefined,
    ): void {
        if (result === null || result === undefined) {
            return;
        }

        const nextDateRange = result.dateRange ?? null;
        if (this.hasSameDateRange(currentDateRange, nextDateRange) && this.hasSameStructuredFilters(currentFilters, result)) {
            return;
        }

        this.searchModel.update(value => ({
            ...value,
            dateRange: nextDateRange,
            mealTypes: result.mealTypes,
            caloriesFrom: result.caloriesFrom,
            caloriesTo: result.caloriesTo,
            hasImage: result.hasImage,
            hasAiSession: result.hasAiSession,
        }));
        this.loadConsumptions(1).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
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

    private formatDateFilterValue(value: Date | null | undefined): string | null {
        return value !== null && value !== undefined ? formatDateInputValue(value) : null;
    }

    protected scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected reloadCurrentPage(): void {
        this.loadConsumptions(this.currentPageIndex() + 1)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();
    }

    private get structuredFilters(): MealListStructuredFilters {
        const model = this.searchModel();
        return {
            dateRange: model.dateRange,
            mealTypes: model.mealTypes,
            caloriesFrom: model.caloriesFrom,
            caloriesTo: model.caloriesTo,
            hasImage: model.hasImage,
            hasAiSession: model.hasAiSession,
        };
    }

    private hasSameStructuredFilters(left: MealListStructuredFilters, right: MealListFiltersDialogResult): boolean {
        return (
            this.hasSameStringValues(left.mealTypes, right.mealTypes) &&
            left.caloriesFrom === right.caloriesFrom &&
            left.caloriesTo === right.caloriesTo &&
            left.hasImage === right.hasImage &&
            left.hasAiSession === right.hasAiSession
        );
    }

    private hasSameStringValues(left: string[], right: string[]): boolean {
        if (left.length !== right.length) {
            return false;
        }

        const leftValues = new Set(left);
        return right.every(value => leftValues.has(value));
    }

    private plannedConsumptions(): Meal[] {
        const today = normalizeStartOfLocalDay(new Date());
        return this.consumptionData.items().filter(item => normalizeStartOfLocalDay(new Date(item.date)).getTime() > today.getTime());
    }

    private currentConsumptions(): Meal[] {
        const today = normalizeStartOfLocalDay(new Date());
        return this.consumptionData.items().filter(item => normalizeStartOfLocalDay(new Date(item.date)).getTime() <= today.getTime());
    }

    private groupByDate(items: Meal[], sortDirection: 'asc' | 'desc'): MealDateGroupView[] {
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

        const groups = Array.from(buckets.values());
        for (const group of groups) {
            group.items.sort((a, b) => this.compareMealDates(a, b, sortDirection));
        }

        return groups.sort((a, b) => {
            const diff = a.date.getTime() - b.date.getTime();
            return sortDirection === 'asc' ? diff : -diff;
        });
    }

    private compareMealDates(left: Meal, right: Meal, sortDirection: 'asc' | 'desc'): number {
        const diff = new Date(left.date).getTime() - new Date(right.date).getTime();
        return sortDirection === 'asc' ? diff : -diff;
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
    mealTypes: string[];
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
    hasAiSession: boolean | null;
};
