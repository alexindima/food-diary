import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { StatisticsService } from '../../services/statistics.service';
import { NavigationService } from '../../services/navigation.service';
import { UserService } from '../../services/user.service';
import { NutrientData } from '../../types/charts.data';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiEntityCardComponent } from 'fd-ui-kit/entity-card/fd-ui-entity-card.component';
import { Consumption, ConsumptionFilters } from '../../types/consumption.data';
import { ConsumptionService } from '../../services/consumption.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDatepicker, FdUiDatepickerInputEvent, FdUiDatepickerModule } from 'fd-ui-kit/material';
import { FdUiInputFieldModule } from 'fd-ui-kit/material';
import { FdUiFormFieldModule } from 'fd-ui-kit/material';
import { FdUiNativeDateModule } from 'fd-ui-kit/material';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { WeightEntriesService } from '../../services/weight-entries.service';
import { WeightEntry } from '../../types/weight-entry.data';
import { WaistEntriesService } from '../../services/waist-entries.service';
import { WaistEntry } from '../../types/waist-entry.data';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { DailyProgressCardComponent } from '../shared/daily-progress-card/daily-progress-card.component';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { MacroSummaryComponent } from '../shared/macro-summary/macro-summary.component';
import { MotivationCardComponent } from '../shared/motivation-card/motivation-card.component';

@Component({
    selector: 'fd-today-consumption',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiEntityCardComponent,
        FdUiDatepickerModule,
        FdUiInputFieldModule,
        FdUiFormFieldModule,
        FdUiNativeDateModule,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        DailyProgressCardComponent,
        LocalizedDatePipe,
        MacroSummaryComponent,
        MotivationCardComponent
    ],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TodayConsumptionComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly navigationService = inject(NavigationService);
    private readonly userService = inject(UserService);
    private readonly consumptionService = inject(ConsumptionService);
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly headerDatePicker = viewChild<FdUiDatepicker<Date>>('headerDatePicker');

    public selectedDate = signal<Date>(this.normalizeDate(new Date()));
    public readonly isTodaySelected = computed(() => {
        const today = this.normalizeDate(new Date());
        return this.selectedDate().getTime() === today.getTime();
    });
    public todayCalories = signal<number>(0);
    public todayFiber = signal<number | null>(null);
    public nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public meals = signal<Consumption[]>([]);
    public isStatsLoading = signal<boolean>(false);
    public isMealsLoading = signal<boolean>(false);
    public dailyGoal = signal<number>(2000);
    public latestWeightEntry = signal<WeightEntry | null>(null);
    public previousWeightEntry = signal<WeightEntry | null>(null);
    public isWeightLoading = signal<boolean>(false);
    public desiredWeight = signal<number | null>(null);
    public isDesiredWeightLoading = signal<boolean>(false);
    public latestWaistEntry = signal<WaistEntry | null>(null);
    public previousWaistEntry = signal<WaistEntry | null>(null);
    public isWaistLoading = signal<boolean>(false);
    public desiredWaist = signal<number | null>(null);
    public isDesiredWaistLoading = signal<boolean>(false);

    public readonly weightMetaText = computed(() => {
        if (this.isDesiredWeightLoading()) {
            return this.translateService.instant('WEIGHT_HISTORY.LOADING');
        }

        const desired = this.desiredWeight();
        if (desired !== null && desired !== undefined) {
            return this.translateService.instant('DASHBOARD.WEIGHT_GOAL', { value: desired });
        }

        return this.translateService.instant('DASHBOARD.WEIGHT_META_EMPTY');
    });

    public readonly weightTrendLabel = computed(() => {
        const latest = this.latestWeightEntry();
        const previous = this.previousWeightEntry();
        if (!latest || !previous) {
            return this.translateService.instant('WEIGHT_HISTORY.NO_PREVIOUS');
        }

        const diff = latest.weight - previous.weight;
        if (Math.abs(diff) < 0.01) {
            return this.translateService.instant('WEIGHT_HISTORY.NO_CHANGE');
        }

        const direction = diff > 0 ? '↑' : '↓';
        return `${direction} ${Math.abs(diff).toFixed(1)} ${this.translateService.instant('DASHBOARD.KG')}`;
    });

    public readonly waistMetaText = computed(() => {
        if (this.isDesiredWaistLoading()) {
            return this.translateService.instant('WAIST_HISTORY.LOADING');
        }

        const desired = this.desiredWaist();
        if (desired !== null && desired !== undefined) {
            return this.translateService.instant('DASHBOARD.WAIST_GOAL', { value: desired });
        }

        return this.translateService.instant('DASHBOARD.WAIST_META_EMPTY');
    });

    public readonly waistTrendLabel = computed(() => {
        const latest = this.latestWaistEntry();
        const previous = this.previousWaistEntry();
        if (!latest || !previous) {
            return this.translateService.instant('WAIST_HISTORY.NO_PREVIOUS');
        }

        const diff = latest.circumference - previous.circumference;
        if (Math.abs(diff) < 0.01) {
            return this.translateService.instant('WAIST_HISTORY.NO_CHANGE');
        }

        const direction = diff > 0 ? '↑' : '↓';
        return `${direction} ${Math.abs(diff).toFixed(1)} ${this.translateService.instant('DASHBOARD.CM')}`;
    });

    public quickActions: DashboardQuickAction[] = [
        {
            icon: 'restaurant',
            titleKey: 'DASHBOARD.ACTIONS.ADD_CONSUMPTION_TITLE',
            descriptionKey: 'DASHBOARD.ACTIONS.ADD_CONSUMPTION_DESCRIPTION',
            buttonKey: 'DASHBOARD.ACTIONS.ADD_CONSUMPTION_BUTTON',
            variant: 'primary',
            fill: 'solid',
            action: () => this.addConsumption(),
        },
        {
            icon: 'add_shopping_cart',
            titleKey: 'DASHBOARD.ACTIONS.ADD_PRODUCT_TITLE',
            descriptionKey: 'DASHBOARD.ACTIONS.ADD_PRODUCT_DESCRIPTION',
            buttonKey: 'DASHBOARD.ACTIONS.ADD_PRODUCT_BUTTON',
            variant: 'secondary',
            fill: 'outline',
            action: () => this.addProduct(),
        },
        {
            icon: 'menu_book',
            titleKey: 'DASHBOARD.ACTIONS.ADD_RECIPE_TITLE',
            descriptionKey: 'DASHBOARD.ACTIONS.ADD_RECIPE_DESCRIPTION',
            buttonKey: 'DASHBOARD.ACTIONS.ADD_RECIPE_BUTTON',
            variant: 'secondary',
            fill: 'outline',
            action: () => this.addRecipe(),
        },
        {
            icon: 'monitoring',
            titleKey: 'DASHBOARD.ACTIONS.STATISTICS_TITLE',
            descriptionKey: 'DASHBOARD.ACTIONS.STATISTICS_DESCRIPTION',
            buttonKey: 'DASHBOARD.ACTIONS.STATISTICS_BUTTON',
            variant: 'primary',
            fill: 'text',
            action: () => this.goToStatistics(),
        },
    ];

    public ngOnInit(): void {
        this.userService
            .getUserCalories()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(goal => this.dailyGoal.set(goal ?? 2000));

        this.fetchDashboardData();
        this.fetchWeightSummary();
        this.fetchWaistSummary();
    }

    public openDatePicker(): void {
        this.headerDatePicker()?.open();
    }

    public handleDateChange(event: FdUiDatepickerInputEvent<Date>): void {
        if (!event.value) {
            return;
        }

        const normalized = this.normalizeDate(event.value);

        if (normalized.getTime() !== this.selectedDate().getTime()) {
            this.selectedDate.set(normalized);
            this.fetchDashboardData();
        }
    }

    public async openWeightHistory(): Promise<void> {
        await this.navigationService.navigateToWeightHistory();
    }

    public async openWaistHistory(): Promise<void> {
        await this.navigationService.navigateToWaistHistory();
    }

    public openConsumption(consumption: Consumption): void {
        void this.navigationService.navigateToConsumptionEdit(consumption.id);
    }

    private fetchDashboardData(): void {
        const targetDate = this.selectedDate();
        this.fetchTodayData(targetDate);
        this.fetchMeals(targetDate);
    }

    private fetchWeightSummary(): void {
        this.isWeightLoading.set(true);
        this.weightEntriesService
            .getEntries({ limit: 2, sort: 'desc' })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: entries => {
                    const [latest, previous] = entries;
                    this.latestWeightEntry.set(latest ?? null);
                    this.previousWeightEntry.set(previous ?? null);
                    this.isWeightLoading.set(false);
                },
                error: () => {
                    this.isWeightLoading.set(false);
                },
            });
        this.fetchDesiredWeight();
    }

    private fetchDesiredWeight(): void {
        this.isDesiredWeightLoading.set(true);
        this.userService
            .getDesiredWeight()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: value => {
                    this.desiredWeight.set(value);
                    this.isDesiredWeightLoading.set(false);
                },
                error: () => {
                    this.isDesiredWeightLoading.set(false);
                },
            });
    }

    private fetchWaistSummary(): void {
        this.isWaistLoading.set(true);
        this.waistEntriesService
            .getEntries({ limit: 2, sort: 'desc' })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: entries => {
                    const [latest, previous] = entries;
                    this.latestWaistEntry.set(latest ?? null);
                    this.previousWaistEntry.set(previous ?? null);
                    this.isWaistLoading.set(false);
                },
                error: () => {
                    this.isWaistLoading.set(false);
                },
            });
        this.fetchDesiredWaist();
    }

    private fetchDesiredWaist(): void {
        this.isDesiredWaistLoading.set(true);
        this.userService
            .getDesiredWaist()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: value => {
                    this.desiredWaist.set(value);
                    this.isDesiredWaistLoading.set(false);
                },
                error: () => {
                    this.isDesiredWaistLoading.set(false);
                },
            });
    }

    private fetchTodayData(date: Date): void {
        this.isStatsLoading.set(true);
        this.statisticsService
            .getAggregatedStatistics({
                dateFrom: date,
                dateTo: date,
                quantizationDays: 1,
            })
            .subscribe({
                next: data => {
                    const stats = data?.[0];
                    this.todayCalories.set(stats?.totalCalories ?? 0);

                    this.nutrientChartData.set({
                        proteins: stats?.averageProteins ?? 0,
                        fats: stats?.averageFats ?? 0,
                        carbs: stats?.averageCarbs ?? 0,
                    });
                    this.todayFiber.set(stats?.averageFiber ?? null);
                    this.isStatsLoading.set(false);
                },
                error: () => {
                    this.isStatsLoading.set(false);
                },
            });
    }

    private fetchMeals(date: Date): void {
        this.isMealsLoading.set(true);
        const filters = this.getDateFilters(date);

        this.consumptionService
            .query(1, 10, filters)
            .subscribe({
                next: response => {
                    this.meals.set(response.data);
                    this.isMealsLoading.set(false);
                },
                error: () => {
                    this.meals.set([]);
                    this.isMealsLoading.set(false);
                },
            });
    }

    private getDateFilters(date: Date): ConsumptionFilters {
        const from = new Date(date);
        from.setHours(0, 0, 0, 0);

        const to = new Date(date);
        to.setHours(23, 59, 59, 999);

        return {
            dateFrom: from.toISOString(),
            dateTo: to.toISOString(),
        };
    }

    private normalizeDate(date: Date): Date {
        const normalized = new Date(date);
        normalized.setHours(0, 0, 0, 0);
        return normalized;
    }

    public async addConsumption(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public async addProduct(): Promise<void> {
        await this.navigationService.navigateToProductAdd();
    }

    public async addRecipe(): Promise<void> {
        await this.navigationService.navigateToRecipeAdd();
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public async manageProducts(): Promise<void> {
        await this.navigationService.navigateToProductList();
    }

    public async goToStatistics(): Promise<void> {
        await this.navigationService.navigateToStatistics();
    }
}

interface DashboardQuickAction {
    icon: string;
    titleKey: string;
    descriptionKey: string;
    buttonKey: string;
    variant: 'primary' | 'secondary' | 'danger';
    fill: 'solid' | 'outline' | 'text';
    action: () => void;
}
