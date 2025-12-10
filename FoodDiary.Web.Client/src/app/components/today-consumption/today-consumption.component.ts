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
import { TranslatePipe } from '@ngx-translate/core';
import { NavigationService } from '../../services/navigation.service';
import { NutrientData } from '../../types/charts.data';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { Consumption } from '../../types/consumption.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDatepicker, FdUiDatepickerInputEvent, FdUiDatepickerModule } from 'fd-ui-kit/material';
import { FdUiInputFieldModule } from 'fd-ui-kit/material';
import { FdUiFormFieldModule } from 'fd-ui-kit/material';
import { FdUiNativeDateModule } from 'fd-ui-kit/material';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { DailyProgressCardComponent } from '../shared/daily-progress-card/daily-progress-card.component';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { MacroSummaryComponent } from '../shared/macro-summary/macro-summary.component';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardSnapshot } from '../../types/dashboard.data';
import { WeightSummaryCardComponent } from '../shared/weight-summary-card/weight-summary-card.component';
import { WaistSummaryCardComponent } from '../shared/waist-summary-card/waist-summary-card.component';
import { ActivityCardComponent } from '../shared/activity-card/activity-card.component';
import { QuickActionsSectionComponent } from '../shared/quick-actions/quick-actions-section/quick-actions-section.component';
import { MealCardComponent } from '../shared/meal-card/meal-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { CalorieGoalDialogComponent } from './dialogs/calorie-goal-dialog/calorie-goal-dialog.component';
import { MacroGoalDialogComponent } from './dialogs/macro-goal-dialog/macro-goal-dialog.component';
import { DailySummaryCardComponent, DailySummaryData } from '../daily-summary-card/daily-summary-card.component';
import {
    ConsumptionRingCardComponent,
    NutrientBar
} from '../shared/consumption-ring-card/consumption-ring-card.component';

@Component({
    selector: 'fd-today-consumption',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
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
        WeightSummaryCardComponent,
        WaistSummaryCardComponent,
        ActivityCardComponent,
        QuickActionsSectionComponent,
        MealCardComponent,
        DailySummaryCardComponent,
        ConsumptionRingCardComponent
    ],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TodayConsumptionComponent implements OnInit {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dashboardService = inject(DashboardService);
    private readonly dialogService = inject(FdUiDialogService);

    private readonly headerDatePicker = viewChild<FdUiDatepicker<Date>>('headerDatePicker');

    public selectedDate = signal<Date>(this.normalizeDate(new Date()));
    public readonly isTodaySelected = computed(() => {
        const today = this.normalizeDate(new Date());
        return this.selectedDate().getTime() === today.getTime();
    });
    public snapshot = signal<DashboardSnapshot | null>(null);
    public isLoading = signal<boolean>(false);

    public readonly dailyGoal = computed(() => this.snapshot()?.dailyGoal ?? 0);
    public readonly todayCalories = computed(() => this.snapshot()?.statistics.totalCalories ?? 0);
    public readonly nutrientChartData = computed<NutrientData>(() => ({
        proteins: this.snapshot()?.statistics.averageProteins ?? 0,
        fats: this.snapshot()?.statistics.averageFats ?? 0,
        carbs: this.snapshot()?.statistics.averageCarbs ?? 0,
    }));
    public readonly proteinGoal = computed(() => this.snapshot()?.statistics.proteinGoal ?? null);
    public readonly fatGoal = computed(() => this.snapshot()?.statistics.fatGoal ?? null);
    public readonly carbGoal = computed(() => this.snapshot()?.statistics.carbGoal ?? null);
    public readonly fiberGoal = computed(() => this.snapshot()?.statistics.fiberGoal ?? null);
    public readonly todayFiber = computed(() => this.snapshot()?.statistics.averageFiber ?? null);
    public readonly meals = computed<Consumption[]>(() => this.snapshot()?.meals.items ?? []);
    public readonly latestWeight = computed(() => this.snapshot()?.weight.latest?.weight ?? null);
    public readonly previousWeight = computed(() => this.snapshot()?.weight.previous?.weight ?? null);
    public readonly desiredWeight = computed(() => this.snapshot()?.weight.desired ?? null);
    public readonly latestWaist = computed(() => this.snapshot()?.waist.latest?.circumference ?? null);
    public readonly previousWaist = computed(() => this.snapshot()?.waist.previous?.circumference ?? null);
    public readonly desiredWaist = computed(() => this.snapshot()?.waist.desired ?? null);
    public readonly weeklyConsumed = computed(() =>
        (this.snapshot()?.weeklyCalories ?? []).reduce((sum, point) => sum + (point?.calories ?? 0), 0)
    );
    public readonly nutrientBars = computed<NutrientBar[]>(() => {
        const snapshot = this.snapshot();

        const proteins = snapshot?.statistics.averageProteins ?? 110;
        const fats = snapshot?.statistics.averageFats ?? 45;
        const carbs = snapshot?.statistics.averageCarbs ?? 180;
        const fiber = snapshot?.statistics.averageFiber ?? 18;

        const proteinGoal = snapshot?.statistics.proteinGoal ?? 140;
        const fatGoal = snapshot?.statistics.fatGoal ?? 70;
        const carbGoal = snapshot?.statistics.carbGoal ?? 250;
        const fiberGoal = snapshot?.statistics.fiberGoal ?? 30;

        return [
            { id: 'protein', label: 'Protein', current: proteins, target: proteinGoal, unit: 'g', colorStart: '#4dabff', colorEnd: '#2563eb' },
            { id: 'carbs', label: 'Carbs', current: carbs, target: carbGoal, unit: 'g', colorStart: '#2dd4bf', colorEnd: '#0ea5e9' },
            { id: 'fats', label: 'Fats', current: fats, target: fatGoal, unit: 'g', colorStart: '#fbbf24', colorEnd: '#f97316' },
            { id: 'fiber', label: 'Fiber', current: fiber, target: fiberGoal, unit: 'g', colorStart: '#fb7185', colorEnd: '#ec4899' },
        ];
    });
    public readonly dailySummaryData = computed<DailySummaryData>(() => {
        const consumed = this.todayCalories() || 0;
        const goal = this.dailyGoal() || 0;
        const percentage = goal > 0 ? Math.round((consumed / goal) * 100) : 0;
        const remaining = goal > 0 ? Math.max(goal - consumed, 0) : undefined;
        const latestMeal = this.meals()[0];
        const weeklyProgress =
            this.snapshot()?.weeklyCalories?.map((point, index, arr) => {
                const date = new Date(point.date);
                const isToday = index === arr.length - 1;
                return {
                    date,
                    calories: point.calories,
                    isToday,
                };
            }) ?? [];
        return {
            mode: 'full',
            sectionTitle: 'Съедено сегодня',
            eatenTodayKcal: consumed,
            goalKcal: goal,
            percentage,
            remainingKcal: remaining,
            weeklyDiffText: undefined,
            weeklyDiffType: 'neutral',
            motivationText: undefined,
            weeklyProgress,
            lastMealTitle: latestMeal?.mealType || undefined,
            lastMealDescription: latestMeal?.comment || undefined,
            showSettings: true,
            onSettingsClick: () => this.openCalorieGoalDialog(),
        };
    });
    public readonly consumptionRingData = computed(() => {
        const snapshot = this.snapshot();
        const dailyGoal = snapshot?.dailyGoal ?? 0;
        const consumedToday = snapshot?.statistics.totalCalories ?? 0;
        const weeklyConsumed = this.weeklyConsumed();
        const hasData = snapshot && (dailyGoal > 0 || consumedToday > 0 || weeklyConsumed > 0);

        if (!hasData) {
            const fallbackGoal = 2000;
            return {
                dailyGoal: fallbackGoal,
                dailyConsumed: 1450,
                weeklyConsumed: 6000,
                weeklyGoal: fallbackGoal * 7,
                nutrientBars: this.nutrientBars(),
            };
        }

        return {
            dailyGoal,
            dailyConsumed: consumedToday,
            weeklyConsumed,
            weeklyGoal: dailyGoal > 0 ? dailyGoal * 7 : 0,
            nutrientBars: this.nutrientBars(),
        };
    });

    public ngOnInit(): void {
        this.loadDashboardSnapshot();
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

    public openCalorieGoalDialog(): void {
        this.dialogService
            .open(CalorieGoalDialogComponent, {
                size: 'sm',
                data: {
                    dailyCalorieTarget: this.dailyGoal() || null,
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(saved => {
                if (saved) {
                    this.loadDashboardSnapshot();
                }
            });
    }

    public openMacroGoalDialog(): void {
        this.dialogService
            .open(MacroGoalDialogComponent, {
                size: 'md',
                data: {
                    proteinTarget: this.proteinGoal(),
                    fatTarget: this.fatGoal(),
                    carbTarget: this.carbGoal(),
                    fiberTarget: this.fiberGoal(),
                },
            })
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(saved => {
                if (saved) {
                    this.loadDashboardSnapshot();
                }
            });
    }

    private fetchDashboardData(): void {
        this.loadDashboardSnapshot();
    }

    private loadDashboardSnapshot(): void {
        const targetDate = this.selectedDate();
        this.isLoading.set(true);

        this.dashboardService
            .getSnapshot(targetDate, 1, 10)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: snapshot => {
                    this.snapshot.set(snapshot);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.snapshot.set(null);
                    this.isLoading.set(false);
                },
            });
    }

    private normalizeDate(date: Date): Date {
        const normalized = new Date(date);
        normalized.setHours(0, 0, 0, 0);
        return normalized;
    }

    public async addConsumption(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }
}
