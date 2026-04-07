import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { auditTime, fromEvent } from 'rxjs';
import { HydrationService } from '../../hydration/api/hydration.service';
import { CyclesService } from '../../cycle-tracking/api/cycles.service';
import { UsdaService } from '../../usda/api/usda.service';
import { DailyMicronutrientSummary } from '../../usda/models/usda.data';
import { CycleResponse } from '../../cycle-tracking/models/cycle.data';
import { Meal } from '../../meals/models/meal.data';
import { GoalsService } from '../../goals/api/goals.service';
import { DashboardService } from '../api/dashboard.service';
import { TdeeService } from '../api/tdee.service';
import { DashboardSnapshot } from '../models/dashboard.data';
import { TdeeInsight } from '../models/tdee-insight.data';
import { getDashboardDateUtc, getHydrationDateUtc, normalizeDate } from './dashboard-date.utils';
import { DashboardLayoutService } from './dashboard-layout.service';
import { createWeightTrendSignals, createWaistTrendSignals } from './dashboard-trend.utils';
import {
    createConsumptionRingSignal,
    createMealPreviewSignal,
    createNutrientBarsSignal,
    placeholderIcon,
    placeholderLabel,
} from './dashboard-nutrition.utils';

@Injectable()
export class DashboardFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly dashboardService = inject(DashboardService);
    private readonly hydrationService = inject(HydrationService);
    private readonly cyclesService = inject(CyclesService);
    private readonly tdeeService = inject(TdeeService);
    private readonly goalsService = inject(GoalsService);
    private readonly usdaService = inject(UsdaService);
    private readonly translateService = inject(TranslateService);
    public readonly layout = inject(DashboardLayoutService);

    private readonly initialized = signal(false);
    private readonly isHydrationUpdating = signal(false);
    private readonly trendDays = 7;

    public readonly selectedDate = signal<Date>(normalizeDate(new Date()));
    public readonly isTodaySelected = computed(() => {
        const today = normalizeDate(new Date());
        return this.selectedDate().getTime() === today.getTime();
    });
    public readonly snapshot = signal<DashboardSnapshot | null>(null);
    public readonly isLoading = signal(false);
    public readonly cycle = signal<CycleResponse | null>(null);
    public readonly isCycleLoading = signal(false);
    public readonly tdeeInsight = signal<TdeeInsight | null>(null);
    public readonly isTdeeLoading = signal(false);
    public readonly micronutrients = signal<DailyMicronutrientSummary | null>(null);
    public readonly isMicronutrientsLoading = signal(false);

    public readonly dailyGoal = computed(() => this.snapshot()?.dailyGoal ?? 0);
    public readonly todayCalories = computed(() => this.snapshot()?.statistics.totalCalories ?? 0);
    public readonly caloriesBurned = computed(() => this.snapshot()?.caloriesBurned ?? 0);
    public readonly meals = computed<Meal[]>(() => this.snapshot()?.meals.items ?? []);
    public readonly latestWeight = computed(() => this.snapshot()?.weight.latest?.weight ?? null);
    public readonly previousWeight = computed(() => this.snapshot()?.weight.previous?.weight ?? null);
    public readonly desiredWeight = computed(() => this.snapshot()?.weight.desired ?? null);
    public readonly latestWaist = computed(() => this.snapshot()?.waist.latest?.circumference ?? null);
    public readonly previousWaist = computed(() => this.snapshot()?.waist.previous?.circumference ?? null);
    public readonly desiredWaist = computed(() => this.snapshot()?.waist.desired ?? null);
    public readonly weeklyConsumed = computed(() =>
        (this.snapshot()?.weeklyCalories ?? []).reduce((sum, point) => sum + (point?.calories ?? 0), 0),
    );
    public readonly hydration = computed(() => this.snapshot()?.hydration ?? null);
    public readonly dailyAdvice = computed(() => this.snapshot()?.advice ?? null);
    private readonly weightTrendPoints = computed(() => this.snapshot()?.weightTrend ?? []);
    private readonly waistTrendPoints = computed(() => this.snapshot()?.waistTrend ?? []);
    public readonly isHydrationLoading = computed(() => this.isLoading() || this.isHydrationUpdating());
    public readonly isWeightTrendLoading = computed(() => this.isLoading());
    public readonly isWaistTrendLoading = computed(() => this.isLoading());
    public readonly isAdviceLoading = computed(() => this.isLoading());

    public readonly weightTrend = createWeightTrendSignals(this.weightTrendPoints, this.latestWeight, this.selectedDate, this.trendDays);
    public readonly waistTrend = createWaistTrendSignals(this.waistTrendPoints, this.latestWaist, this.selectedDate, this.trendDays);
    public readonly nutrientBars = createNutrientBarsSignal(this.snapshot);
    public readonly consumptionRingData = createConsumptionRingSignal(this.snapshot, this.weeklyConsumed, this.nutrientBars);
    public readonly mealPreviewEntries = createMealPreviewSignal(this.meals, this.isTodaySelected);

    public readonly placeholderIcon = placeholderIcon;
    public readonly placeholderLabel = placeholderLabel;

    public initialize(): void {
        if (this.initialized()) {
            return;
        }

        this.initialized.set(true);
        this.loadDashboardSnapshot();
        this.loadCycle();
        this.loadTdeeInsight();
        this.loadMicronutrients();

        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.loadDashboardSnapshot(false));

        if (typeof window !== 'undefined') {
            this.layout.updateViewportWidth(window.innerWidth);
            fromEvent(window, 'resize')
                .pipe(auditTime(150), takeUntilDestroyed(this.destroyRef))
                .subscribe(() => this.layout.updateViewportWidth(window.innerWidth));
        }
    }

    public setSelectedDate(date: Date): void {
        const normalized = normalizeDate(date);
        if (normalized.getTime() === this.selectedDate().getTime()) {
            return;
        }

        this.selectedDate.set(normalized);
        this.loadDashboardSnapshot();
        this.loadMicronutrients();
    }

    public addHydration(amount: number): void {
        this.isHydrationUpdating.set(true);
        const targetDate = getHydrationDateUtc(this.selectedDate());
        this.hydrationService
            .addEntry(amount, targetDate)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => this.loadDashboardSnapshot(false, true),
                error: () => this.isHydrationUpdating.set(false),
            });
    }

    public applyTdeeGoal(target: number): void {
        this.goalsService
            .updateGoals({ dailyCalorieTarget: target })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.loadDashboardSnapshot(false);
                this.loadTdeeInsight();
            });
    }

    public reload(showLoader = true): void {
        this.loadDashboardSnapshot(showLoader);
    }

    private loadDashboardSnapshot(showLoader = true, clearHydrationUpdate = false): void {
        const targetDate = getDashboardDateUtc(this.selectedDate());
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
                    this.layout.initializeLayout(snapshot?.dashboardLayout ?? null);
                    this.isLoading.set(false);
                    if (clearHydrationUpdate) {
                        this.isHydrationUpdating.set(false);
                    }
                },
                error: () => {
                    this.snapshot.set(null);
                    this.layout.initializeLayout(null);
                    this.isLoading.set(false);
                    if (clearHydrationUpdate) {
                        this.isHydrationUpdating.set(false);
                    }
                },
            });
    }

    private loadCycle(): void {
        this.isCycleLoading.set(true);
        this.cyclesService
            .getCurrent()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: cycle => {
                    this.cycle.set(cycle);
                    this.isCycleLoading.set(false);
                },
                error: () => {
                    this.cycle.set(null);
                    this.isCycleLoading.set(false);
                },
            });
    }

    private loadTdeeInsight(): void {
        this.isTdeeLoading.set(true);
        this.tdeeService
            .getInsight()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: insight => {
                    this.tdeeInsight.set(insight);
                    this.isTdeeLoading.set(false);
                },
                error: () => {
                    this.tdeeInsight.set(null);
                    this.isTdeeLoading.set(false);
                },
            });
    }

    private loadMicronutrients(): void {
        this.isMicronutrientsLoading.set(true);
        const targetDate = getDashboardDateUtc(this.selectedDate()).toISOString();
        this.usdaService
            .getDailyMicronutrients(targetDate)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: summary => {
                    this.micronutrients.set(summary);
                    this.isMicronutrientsLoading.set(false);
                },
                error: () => {
                    this.micronutrients.set(null);
                    this.isMicronutrientsLoading.set(false);
                },
            });
    }

    private getCurrentLocale(): string {
        const lang = this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
        return lang.split(/[-_]/)[0];
    }
}
