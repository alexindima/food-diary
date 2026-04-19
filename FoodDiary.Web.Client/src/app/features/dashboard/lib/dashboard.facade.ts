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
import { FastingSession } from '../../fasting/models/fasting.data';
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
import { runTrackedRequest } from '../../../shared/lib/run-tracked-request';

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
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    public readonly selectedDate = signal<Date>(normalizeDate(new Date()));
    public readonly now = signal(new Date());
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
    public readonly currentFastingSession = computed<FastingSession | null>(() => this.snapshot()?.currentFastingSession ?? null);
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
    public readonly fastingIsActive = computed(() => {
        const session = this.currentFastingSession();
        return session !== null && session.endedAtUtc === null;
    });
    public readonly fastingElapsedMs = computed(() => {
        const session = this.currentFastingSession();
        if (!session) {
            return 0;
        }

        const start = new Date(session.startedAtUtc).getTime();
        const end = session.endedAtUtc ? new Date(session.endedAtUtc).getTime() : this.now().getTime();
        return Math.max(0, end - start);
    });
    public readonly fastingTotalMs = computed(() => {
        const session = this.currentFastingSession();
        return session ? session.plannedDurationHours * 3600_000 : 0;
    });
    public readonly fastingProgressPercent = computed(() => {
        const total = this.fastingTotalMs();
        if (total <= 0) {
            return 0;
        }

        return Math.min((this.fastingElapsedMs() / total) * 100, 100);
    });
    public readonly fastingElapsedFormatted = computed(() => this.formatDuration(this.fastingElapsedMs()));
    public readonly fastingRemainingFormatted = computed(() => {
        const remaining = Math.max(0, this.fastingTotalMs() - this.fastingElapsedMs());
        return this.formatDuration(remaining);
    });
    public readonly fastingIsOvertime = computed(() => this.fastingElapsedMs() > this.fastingTotalMs());

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
        const targetDate = getHydrationDateUtc(this.selectedDate());
        runTrackedRequest(this.destroyRef, this.isHydrationUpdating, this.hydrationService.addEntry(amount, targetDate), {
            next: () => this.loadDashboardSnapshot(false, true),
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
        this.loadMicronutrients();
    }

    private loadDashboardSnapshot(showLoader = true, clearHydrationUpdate = false): void {
        const targetDate = getDashboardDateUtc(this.selectedDate());
        const locale = this.getCurrentLocale();

        if (showLoader) {
            this.isLoading.set(true);
        }

        runTrackedRequest(this.destroyRef, this.isLoading, this.dashboardService.getSnapshot(targetDate, 1, 10, locale, this.trendDays), {
            next: snapshot => {
                this.snapshot.set(snapshot);
                this.layout.initializeLayout(snapshot?.dashboardLayout ?? null);
                this.syncFastingTimer(snapshot);
                if (clearHydrationUpdate) {
                    this.isHydrationUpdating.set(false);
                }
            },
            error: () => {
                this.snapshot.set(null);
                this.layout.initializeLayout(null);
                this.stopFastingTimer();
                if (clearHydrationUpdate) {
                    this.isHydrationUpdating.set(false);
                }
            },
        });
    }

    private loadCycle(): void {
        runTrackedRequest(this.destroyRef, this.isCycleLoading, this.cyclesService.getCurrent(), {
            next: cycle => this.cycle.set(cycle),
            error: () => this.cycle.set(null),
        });
    }

    private loadTdeeInsight(): void {
        runTrackedRequest(this.destroyRef, this.isTdeeLoading, this.tdeeService.getInsight(), {
            next: insight => this.tdeeInsight.set(insight),
            error: () => this.tdeeInsight.set(null),
        });
    }

    private loadMicronutrients(): void {
        const targetDate = getDashboardDateUtc(this.selectedDate()).toISOString();
        runTrackedRequest(this.destroyRef, this.isMicronutrientsLoading, this.usdaService.getDailyMicronutrients(targetDate), {
            next: summary => this.micronutrients.set(summary),
            error: () => this.micronutrients.set(null),
        });
    }

    private getCurrentLocale(): string {
        const lang = this.translateService.currentLang || this.translateService.getDefaultLang() || 'en';
        return lang.split(/[-_]/)[0];
    }

    private syncFastingTimer(snapshot: DashboardSnapshot | null): void {
        const session = snapshot?.currentFastingSession;
        if (session && session.endedAtUtc === null) {
            this.startFastingTimer();
            return;
        }

        this.stopFastingTimer();
    }

    private startFastingTimer(): void {
        if (this.timerInterval !== null) {
            return;
        }

        this.now.set(new Date());
        this.timerInterval = setInterval(() => this.now.set(new Date()), 1000);
        this.destroyRef.onDestroy(() => this.stopFastingTimer());
    }

    private stopFastingTimer(): void {
        if (this.timerInterval === null) {
            return;
        }

        clearInterval(this.timerInterval);
        this.timerInterval = null;
    }

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }
}
