
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
import { TranslateService } from '@ngx-translate/core';
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
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { CalorieGoalDialogComponent } from './dialogs/calorie-goal-dialog/calorie-goal-dialog.component';
import {
    DashboardSummaryCardComponent,
    NutrientBar
} from '../shared/dashboard-summary-card/dashboard-summary-card.component';
import { HydrationService } from '../../services/hydration.service';
import { HydrationCardComponent } from '../shared/hydration-card/hydration-card.component';
import { WeightTrendCardComponent, WeightTrendPoint } from '../shared/weight-trend-card/weight-trend-card.component';
import { DailyAdviceCardComponent } from '../shared/daily-advice-card/daily-advice-card.component';
import { MealsPreviewComponent, MealPreviewEntry } from '../shared/meals-preview/meals-preview.component';

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
    DashboardSummaryCardComponent,
    HydrationCardComponent,
    WeightTrendCardComponent,
    DailyAdviceCardComponent,
    MealsPreviewComponent
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
    private readonly translateService = inject(TranslateService);

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
    private readonly isHydrationUpdating = signal<boolean>(false);
    private readonly trendDays = 7;
    public readonly hydration = computed(() => this.snapshot()?.hydration ?? null);
    public readonly dailyAdvice = computed(() => this.snapshot()?.advice ?? null);
    private readonly weightTrendPoints = computed(() => this.snapshot()?.weightTrend ?? []);
    private readonly waistTrendPoints = computed(() => this.snapshot()?.waistTrend ?? []);
    public readonly isHydrationLoading = computed(() => this.isLoading() || this.isHydrationUpdating());
    public readonly isWeightTrendLoading = computed(() => this.isLoading());
    public readonly isWaistTrendLoading = computed(() => this.isLoading());
    public readonly isAdviceLoading = computed(() => this.isLoading());
    private readonly mealSlots: MealSlot[] = ['BREAKFAST', 'LUNCH', 'DINNER'];
    public readonly mealPreviewEntries = computed<MealPreviewEntry[]>(() => {
        const meals = [...(this.meals() ?? [])];

        if (!this.isTodaySelected()) {
            return meals.map(meal => ({
                meal,
                slot: meal.mealType ?? undefined,
            }));
        }

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

        return result.map(entry => ({
            meal: entry.meal ?? null,
            slot: entry.slot,
            icon: this.placeholderIcon(entry.slot),
            labelKey: this.placeholderLabel(entry.slot),
        }));
    });

    public readonly nutrientBars = computed<NutrientBar[]>(() => {
        const snapshot = this.snapshot();

        if (!snapshot) {
            return [];
        }

        return [
            {
                id: 'protein',
                label: 'Protein',
                current: snapshot.statistics.averageProteins ?? 0,
                target: snapshot.statistics.proteinGoal ?? 0,
                unit: 'g',
                colorStart: '#4dabff',
                colorEnd: '#2563eb',
            },
            {
                id: 'carbs',
                label: 'Carbs',
                current: snapshot.statistics.averageCarbs ?? 0,
                target: snapshot.statistics.carbGoal ?? 0,
                unit: 'g',
                colorStart: '#2dd4bf',
                colorEnd: '#0ea5e9',
            },
            {
                id: 'fats',
                label: 'Fats',
                current: snapshot.statistics.averageFats ?? 0,
                target: snapshot.statistics.fatGoal ?? 0,
                unit: 'g',
                colorStart: '#fbbf24',
                colorEnd: '#f97316',
            },
            {
                id: 'fiber',
                label: 'Fiber',
                current: snapshot.statistics.averageFiber ?? 0,
                target: snapshot.statistics.fiberGoal ?? 0,
                unit: 'g',
                colorStart: '#fb7185',
                colorEnd: '#ec4899',
            },
        ];
    });
    public readonly consumptionRingData = computed(() => {
        const snapshot = this.snapshot();
        const dailyGoal = snapshot?.dailyGoal ?? 0;
        const consumedToday = snapshot?.statistics.totalCalories ?? 0;
        const weeklyConsumed = this.weeklyConsumed();

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

        this.translateService.onLangChange
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.loadDashboardSnapshot(false));
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
    }

    private loadDashboardSnapshot(showLoader = true, clearHydrationUpdate = false): void {
        const targetDate = this.getDashboardDateUtc(this.selectedDate());
        const locale = this.getCurrentLocale();

        if (showLoader) {
            this.isLoading.set(true);
        }

        this.dashboardService
            .getSnapshot(targetDate, 1, 10, locale, this.trendDays)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: snapshot => {
                    this.snapshot.set(snapshot);
                    this.isLoading.set(false);
                    if (clearHydrationUpdate) {
                        this.isHydrationUpdating.set(false);
                    }
                },
                error: () => {
                    this.snapshot.set(null);
                    this.isLoading.set(false);
                    if (clearHydrationUpdate) {
                        this.isHydrationUpdating.set(false);
                    }
                },
            });
    }

    private normalizeDate(date: Date): Date {
        const normalized = new Date(date);
        normalized.setHours(0, 0, 0, 0);
        return normalized;
    }

    public async addConsumption(mealType?: string | null): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd(mealType ?? undefined);
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public addHydration(amount: number): void {
        this.isHydrationUpdating.set(true);
        const targetDate = this.getHydrationDateUtc(this.selectedDate());
        this.hydrationService
            .addEntry(amount, targetDate)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => this.loadDashboardSnapshot(false, true),
                error: () => this.isHydrationUpdating.set(false),
            });
    }
    private getWeightTrendRange(): { start: Date; end: Date } {
        const end = this.selectedDate();
        const start = new Date(end);
        start.setDate(start.getDate() - (this.trendDays - 1));

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

    private getDashboardDateUtc(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private getHydrationDateUtc(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 12, 0, 0));
    }

    private getCurrentLocale(): string {
        const lang = this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
        return lang.split(/[-_]/)[0];
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
