
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
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardSnapshot } from '../../types/dashboard.data';
import { MealCardComponent } from '../shared/meal-card/meal-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { CalorieGoalDialogComponent } from './dialogs/calorie-goal-dialog/calorie-goal-dialog.component';
import {
    ConsumptionRingCardComponent,
    NutrientBar
} from '../shared/consumption-ring-card/consumption-ring-card.component';
import { HydrationService } from '../../services/hydration.service';
import { HydrationCardComponent } from '../shared/hydration-card/hydration-card.component';
import { HydrationDaily } from '../../types/hydration.data';
import { WeightEntriesService } from '../../services/weight-entries.service';
import { WeightEntrySummaryPoint } from '../../types/weight-entry.data';
import { WeightTrendCardComponent, WeightTrendPoint } from '../shared/weight-trend-card/weight-trend-card.component';
import { WaistEntriesService } from '../../services/waist-entries.service';
import { WaistEntrySummaryPoint } from '../../types/waist-entry.data';

type MealSlot = 'BREAKFAST' | 'LUNCH' | 'DINNER';

@Component({
    selector: 'fd-dashboard',
    standalone: true,
    imports: [
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
    LocalizedDatePipe,
    MealCardComponent,
    ConsumptionRingCardComponent,
    HydrationCardComponent,
    WeightTrendCardComponent
],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dashboardService = inject(DashboardService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly hydrationService = inject(HydrationService);
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly waistEntriesService = inject(WaistEntriesService);

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
    public readonly hydration = signal<HydrationDaily | null>(null);
    public readonly isHydrationLoading = signal<boolean>(false);
    public readonly weightTrendPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly isWeightTrendLoading = signal<boolean>(false);
    public readonly waistTrendPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly isWaistTrendLoading = signal<boolean>(false);
    private readonly mealSlots: MealSlot[] = ['BREAKFAST', 'LUNCH', 'DINNER'];
    public readonly displayedMeals = computed(() => {
        const meals = [...(this.meals() ?? [])];
        const result: { meal: Consumption | null; slot?: MealSlot }[] = [];

        for (const slot of this.mealSlots) {
            const index = meals.findIndex(m => (m.mealType ?? '').toUpperCase() === slot);
            if (index >= 0) {
                result.push({ meal: meals[index], slot });
                meals.splice(index, 1);
            } else {
                result.push({ meal: null, slot });
            }
        }

        for (const meal of meals) {
            result.push({ meal, slot: (meal.mealType ?? '').toUpperCase() as MealSlot | undefined });
        }

        return result;
    });

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
    public readonly weightTrendSeries = computed<WeightTrendPoint[]>(() =>
        this.weightTrendPoints().map(point => ({
            date: point.dateFrom,
            value: point.averageWeight > 0 ? point.averageWeight : null,
        })).length
            ? this.weightTrendPoints().map(point => ({
                  date: point.dateFrom,
                  value: point.averageWeight > 0 ? point.averageWeight : null,
              }))
            : this.buildFallbackWeightTrend(),
    );
    public readonly weightTrendChange = computed(() => {
        const ordered = [...this.weightTrendSeries()].sort(
            (a, b) => new Date(a.date as string).getTime() - new Date(b.date as string).getTime(),
        );
        const first = ordered.find(point => point.value !== null && point.value !== undefined);
        const last = [...ordered].reverse().find(point => point.value !== null && point.value !== undefined);

        if (!first || !last) {
            return null;
        }

        const diff = (last.value ?? 0) - (first.value ?? 0);
        return Math.round(diff * 10) / 10;
    });
    public readonly weightTrendCurrent = computed(() => {
        const ordered = [...this.weightTrendSeries()].sort(
            (a, b) => new Date(a.date as string).getTime() - new Date(b.date as string).getTime(),
        );
        const last = [...ordered].reverse().find(point => point.value !== null && point.value !== undefined);
        return last?.value ?? this.latestWeight() ?? null;
    });

    public ngOnInit(): void {
        this.loadDashboardSnapshot();
        this.loadHydration();
        this.loadWeightTrend();
        this.loadWaistTrend();
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

    private fetchDashboardData(): void {
        this.loadDashboardSnapshot();
        this.loadHydration();
        this.loadWeightTrend();
        this.loadWaistTrend();
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

    private loadHydration(): void {
        const targetDate = this.selectedDate();
        this.isHydrationLoading.set(true);

        this.hydrationService
            .getDaily(targetDate)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: daily => {
                    this.hydration.set(daily);
                    this.isHydrationLoading.set(false);
                },
                error: () => {
                    this.hydration.set({ dateUtc: targetDate.toISOString(), totalMl: 0, goalMl: null });
                    this.isHydrationLoading.set(false);
                },
            });
    }

    private normalizeDate(date: Date): Date {
        const normalized = new Date(date);
        normalized.setHours(0, 0, 0, 0);
        return normalized;
    }

    public async addConsumption(mealType?: string): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd(mealType);
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public addHydration(amount: number): void {
        this.isHydrationLoading.set(true);
        this.hydrationService
            .addEntry(amount)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => this.loadHydration(),
                error: () => this.isHydrationLoading.set(false),
            });
    }

    private loadWeightTrend(): void {
        const { start, end } = this.getWeightTrendRange();
        this.isWeightTrendLoading.set(true);

        this.weightEntriesService
            .getSummary({
                dateFrom: start.toISOString(),
                dateTo: end.toISOString(),
                quantizationDays: 1,
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: points => {
                    this.weightTrendPoints.set(points);
                    this.isWeightTrendLoading.set(false);
                },
                error: () => {
                    this.weightTrendPoints.set([]);
                    this.isWeightTrendLoading.set(false);
                },
            });
    }

    private getWeightTrendRange(): { start: Date; end: Date } {
        const end = this.selectedDate();
        const start = new Date(end);
        start.setDate(start.getDate() - 6);

        return {
            start: this.normalizeStartOfDayUtc(start),
            end: this.normalizeEndOfDayUtc(end),
        };
    }

    private normalizeStartOfDayUtc(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private normalizeEndOfDayUtc(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999));
    }

    private buildFallbackWeightTrend(): WeightTrendPoint[] {
        const latest = this.latestWeight();
        if (!latest) {
            return [];
        }

        const { start } = this.getWeightTrendRange();
        const points: WeightTrendPoint[] = [];

        for (let i = 0; i < 7; i++) {
            const date = new Date(start);
            date.setDate(start.getDate() + i);
            points.push({
                date: date.toISOString(),
                value: latest,
            });
        }

        return points;
    }

    public readonly waistTrendSeries = computed<WeightTrendPoint[]>(() =>
        this.waistTrendPoints().map(point => ({
            date: point.dateFrom,
            value: point.averageCircumference > 0 ? point.averageCircumference : null,
        })).length
            ? this.waistTrendPoints().map(point => ({
                  date: point.dateFrom,
                  value: point.averageCircumference > 0 ? point.averageCircumference : null,
              }))
            : this.buildFallbackWaistTrend(),
    );
    public readonly waistTrendChange = computed(() => {
        const ordered = [...this.waistTrendSeries()].sort(
            (a, b) => new Date(a.date as string).getTime() - new Date(b.date as string).getTime(),
        );
        const first = ordered.find(point => point.value !== null && point.value !== undefined);
        const last = [...ordered].reverse().find(point => point.value !== null && point.value !== undefined);

        if (!first || !last) {
            return null;
        }

        const diff = (last.value ?? 0) - (first.value ?? 0);
        return Math.round(diff * 10) / 10;
    });
    public readonly waistTrendCurrent = computed(() => {
        const ordered = [...this.waistTrendSeries()].sort(
            (a, b) => new Date(a.date as string).getTime() - new Date(b.date as string).getTime(),
        );
        const last = [...ordered].reverse().find(point => point.value !== null && point.value !== undefined);
        return last?.value ?? this.latestWaist() ?? null;
    });

    private loadWaistTrend(): void {
        const { start, end } = this.getWeightTrendRange();
        this.isWaistTrendLoading.set(true);

        this.waistEntriesService
            .getSummary({
                dateFrom: start.toISOString(),
                dateTo: end.toISOString(),
                quantizationDays: 1,
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: points => {
                    this.waistTrendPoints.set(points);
                    this.isWaistTrendLoading.set(false);
                },
                error: () => {
                    this.waistTrendPoints.set([]);
                    this.isWaistTrendLoading.set(false);
                },
            });
    }

    private buildFallbackWaistTrend(): WeightTrendPoint[] {
        const latest = this.latestWaist();
        if (!latest) {
            return [];
        }

        const { start } = this.getWeightTrendRange();
        const points: WeightTrendPoint[] = [];

        for (let i = 0; i < 7; i++) {
            const date = new Date(start);
            date.setDate(start.getDate() + i);
            points.push({
                date: date.toISOString(),
                value: latest,
            });
        }

        return points;
    }

    public placeholderIcon(slot?: MealSlot | string): string {
        switch (slot) {
            case 'BREAKFAST':
                return 'wb_sunny';
            case 'LUNCH':
                return 'lunch_dining';
            case 'DINNER':
                return 'nights_stay';
            case 'SNACK':
                return 'cookie';
            case 'OTHER':
                return 'more_horiz';
            default:
                return 'restaurant_menu';
        }
    }

    public placeholderLabel(slot?: MealSlot | string): string {
        if (!slot) {
            return 'MEAL_CARD.MEAL_TYPES.OTHER';
        }
        return `MEAL_CARD.MEAL_TYPES.${slot}`;
    }
}
