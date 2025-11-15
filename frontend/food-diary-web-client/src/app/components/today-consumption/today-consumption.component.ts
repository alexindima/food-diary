import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    ViewChild,
    computed,
    inject,
    signal,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { StatisticsService } from '../../services/statistics.service';
import { NavigationService } from '../../services/navigation.service';
import { UserService } from '../../services/user.service';
import { NutrientChartData } from '../../types/charts.data';
import { DynamicProgressBarComponent } from '../shared/dynamic-progress-bar/dynamic-progress-bar.component';
import { FdUiCardComponent } from '../../ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from '../../ui-kit/button/fd-ui-button.component';
import { FdUiEntityCardComponent } from '../../ui-kit/entity-card/fd-ui-entity-card.component';
import { Consumption, ConsumptionFilters } from '../../types/consumption.data';
import { ConsumptionService } from '../../services/consumption.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDatepicker, MatDatepickerInputEvent, MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';

interface DashboardQuickAction {
    icon: string;
    titleKey: string;
    descriptionKey: string;
    buttonKey: string;
    variant: 'primary' | 'secondary' | 'danger';
    fill: 'solid' | 'outline' | 'text';
    action: () => void;
}

@Component({
    selector: 'fd-today-consumption',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        DynamicProgressBarComponent,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiEntityCardComponent,
        MatDatepickerModule,
        MatInputModule,
        MatFormFieldModule,
        MatNativeDateModule,
        MatIconModule,
    ],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TodayConsumptionComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly navigationService = inject(NavigationService);
    private readonly userService = inject(UserService);
    private readonly consumptionService = inject(ConsumptionService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('headerDatePicker') private headerDatePicker?: MatDatepicker<Date>;

    public selectedDate = signal<Date>(this.normalizeDate(new Date()));
    public todayCalories = signal<number>(0);
    public todayFiber = signal<number | null>(null);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public meals = signal<Consumption[]>([]);
    public isStatsLoading = signal<boolean>(false);
    public isMealsLoading = signal<boolean>(false);
    public dailyGoal = signal<number>(2000);

    public readonly macroSummary = computed(() => ([
        {
            labelKey: 'PRODUCT_LIST.PROTEINS',
            value: this.nutrientChartData().proteins,
            unitKey: 'PRODUCT_LIST.GRAMS' as const,
        },
        {
            labelKey: 'PRODUCT_LIST.FATS',
            value: this.nutrientChartData().fats,
            unitKey: 'PRODUCT_LIST.GRAMS' as const,
        },
        {
            labelKey: 'PRODUCT_LIST.CARBS',
            value: this.nutrientChartData().carbs,
            unitKey: 'PRODUCT_LIST.GRAMS' as const,
        },
        {
            labelKey: 'SHARED.NUTRIENTS_SUMMARY.FIBER',
            value: this.todayFiber() ?? 0,
            unitKey: 'PRODUCT_LIST.GRAMS' as const,
        },
    ]));

    public readonly progressPercent = computed(() => {
        const goal = this.dailyGoal();
        if (!goal || goal <= 0) {
            return 0;
        }

        const percent = (this.todayCalories() / goal) * 100;
        return Math.round(Math.max(0, percent));
    });

    public readonly remainingCalories = computed(() => {
        const remaining = this.dailyGoal() - this.todayCalories();
        return remaining > 0 ? remaining : 0;
    });

    public readonly motivationKey = computed(() => {
        const percent = this.progressPercent();

        if (percent < 25) {
            return 'DASHBOARD.MOTIVATION.EARLY';
        }

        if (percent < 60) {
            return 'DASHBOARD.MOTIVATION.MID';
        }

        if (percent < 100) {
            return 'DASHBOARD.MOTIVATION.NEARLY';
        }

        return 'DASHBOARD.MOTIVATION.ABOVE';
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
    }

    public openDatePicker(): void {
        this.headerDatePicker?.open();
    }

    public handleDateChange(event: MatDatepickerInputEvent<Date>): void {
        if (!event.value) {
            return;
        }

        const normalized = this.normalizeDate(event.value);

        if (normalized.getTime() !== this.selectedDate().getTime()) {
            this.selectedDate.set(normalized);
            this.fetchDashboardData();
        }
    }

    public openConsumption(consumption: Consumption): void {
        void this.navigationService.navigateToConsumptionEdit(consumption.id);
    }

    private fetchDashboardData(): void {
        const targetDate = this.selectedDate();
        this.fetchTodayData(targetDate);
        this.fetchMeals(targetDate);
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
