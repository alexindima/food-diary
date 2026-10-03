import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Observable, PartialObserver } from 'rxjs';
import { finalize, firstValueFrom, map, of, switchMap } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { resolveMealTypeByTime } from '../../../shared/lib/meal-type.util';
import { RequestStateController } from '../../../shared/lib/request-state';
import { runTrackedRequest } from '../../../shared/lib/run-tracked-request';
import type { CycleResponse } from '../../../shared/models/cycle.data';
import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import type { FastingSession } from '../../../shared/models/fasting.data';
import type { Meal } from '../../../shared/models/meal.data';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { CALORIE_GOAL_ACTIONS } from '../../goals/contracts/calorie-goal-actions';
import { HYDRATION_ACTIONS } from '../../hydration/contracts/hydration-actions';
import { FAVORITE_MEAL_ACTIONS, MEAL_ACTIONS } from '../../meals/contracts/meal-actions';
import type { MealDetailActionResult } from '../../meals/contracts/meal-detail-actions';
import { DashboardService } from '../api/dashboard.service';
import type { TdeeInsightDialogComponent as TdeeInsightDialogComponentType } from '../dialogs/tdee-insight-dialog/tdee-insight-dialog';
import type {
    TdeeInsightDialogAction,
    TdeeInsightDialogData,
} from '../dialogs/tdee-insight-dialog/tdee-insight-dialog-lib/tdee-insight-dialog.types';
import { getDashboardDateUtc, getHydrationDateUtc, normalizeDate } from './dashboard-date.utils';
import { DASHBOARD_TREND_DAYS } from './dashboard-facade.config';
import { DashboardLayoutService } from './dashboard-layout.service';
import { DashboardLocalDayFacade } from './dashboard-local-day.facade';
import {
    createMealPreviewSignal,
    createMealRingSignal,
    createNutrientBarsSignal,
    placeholderIcon,
    placeholderLabel,
} from './dashboard-nutrition.utils';
import { createWaistTrendSignals, createWeightTrendSignals } from './dashboard-trend.utils';
import { resolveDashboardNutritionInsight } from './nutrition-insight.policy';

@Injectable()
export class DashboardFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly localDay = inject(DashboardLocalDayFacade);
    private followsToday = true;
    private readonly mealService = inject(MEAL_ACTIONS);
    private readonly navigationService = inject(NavigationService);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly favoriteMealService = inject(FAVORITE_MEAL_ACTIONS);
    private readonly toastService = inject(FdUiToastService);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set());
    private readonly favoriteStates = signal<Record<string, { isFavorite: boolean; favoriteMealId: string | null }>>({});
    private readonly dashboardService = inject(DashboardService);
    private readonly hydrationService = inject(HYDRATION_ACTIONS);
    private readonly goalsService = inject(CALORIE_GOAL_ACTIONS);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private tdeeGoalRequest: Promise<boolean> | null = null;
    public readonly isApplyingTdeeGoal = signal(false);
    public readonly layout = inject(DashboardLayoutService);
    public async openMealDetailsAsync(mealId: string): Promise<void> {
        const meal = this.meals().find(item => item.id === mealId);
        if (meal === undefined) {
            return;
        }
        const { MealDetailComponent } = await import('../../meals/contracts/meal-detail');
        const result = await firstValueFrom(
            this.dialogService
                .open<InstanceType<typeof MealDetailComponent>, Meal, MealDetailActionResult>(MealDetailComponent, {
                    preset: 'detail',
                    data: meal,
                })
                .afterClosed(),
        );
        if (result === undefined) {
            return;
        }
        if (result.favoriteChanged || result.action === 'FavoriteChanged') {
            this.favoriteStates.set({});
            this.reload(false);
        }
        if (result.action === 'Edit') {
            await this.navigationService.navigateToMealEditAsync(result.id);
            return;
        }
        if (result.action === 'FavoriteChanged') {
            return;
        }
        try {
            const targetDate = new Date();
            await firstValueFrom<Meal | void>(
                result.action === 'Repeat'
                    ? this.mealService.repeat(result.id, targetDate.toISOString(), resolveMealTypeByTime(targetDate))
                    : this.mealService.deleteById(result.id),
            );
            this.invalidation.reportMealMutation();
            this.reload(false);
        } catch {
            this.toastService.error(this.translateService.instant('MEAL_LIST.OPERATION_ERROR_MESSAGE'));
        }
    }
    public toggleMealFavorite(mealId: string): void {
        const meal = this.meals().find(item => item.id === mealId);
        if (meal === undefined || this.favoriteLoadingIds().has(mealId)) {
            return;
        }
        this.favoriteLoadingIds.update(ids => new Set([...ids, mealId]));
        const request$: Observable<{ isFavorite: boolean; favoriteMealId: string | null }> =
            meal.isFavorite === true
                ? ((meal.favoriteMealId?.length ?? 0) > 0
                      ? of(meal.favoriteMealId)
                      : this.favoriteMealService
                            .getLookupPage()
                            .pipe(map(favorites => favorites.find(favorite => favorite.mealId === mealId)?.id))
                  ).pipe(
                      switchMap(id =>
                          id !== undefined && id !== null && id.length > 0 ? this.favoriteMealService.remove(id) : of(undefined),
                      ),
                      map(() => ({ isFavorite: false, favoriteMealId: null })),
                  )
                : this.favoriteMealService.add(mealId).pipe(map(favorite => ({ isFavorite: true, favoriteMealId: favorite.id })));
        request$
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.favoriteLoadingIds.update(ids => new Set([...ids].filter(id => id !== mealId)));
                }),
            )
            .subscribe({
                next: state => {
                    this.favoriteStates.update(states => ({ ...states, [mealId]: state }));
                },
                error: () => this.toastService.error(this.translateService.instant('MEAL_LIST.OPERATION_ERROR_MESSAGE')),
            });
    }

    private readonly initialized = signal(false);
    private readonly isHydrationUpdating = signal(false);
    private readonly isHydrationRefreshing = signal(false);
    private readonly trendDays = DASHBOARD_TREND_DAYS;
    private readonly snapshotRequest = new RequestStateController<DashboardSnapshot, 'DASHBOARD.LOAD_ERROR'>();

    public readonly selectedDate = signal<Date>(normalizeDate(new Date()));
    public readonly isTodaySelected = computed(() => {
        const today = this.localDay.today();
        return this.selectedDate().getTime() === today.getTime();
    });
    public readonly snapshot = this.snapshotRequest.data;
    public readonly isLoading = this.snapshotRequest.isLoading;
    public readonly hasSnapshot = this.snapshotRequest.hasData;
    public readonly loadError = this.snapshotRequest.error;
    public readonly cycle = computed<CycleResponse | null>(() => this.snapshot()?.currentCycle ?? null);
    public readonly isCycleLoading = computed(() => this.isLoading());
    public readonly tdeeInsight = computed(() => this.snapshot()?.tdeeInsight ?? null);

    public readonly dailyGoal = computed(() => this.snapshot()?.dailyGoal ?? 0);
    public readonly todayCalories = computed(() => this.snapshot()?.statistics.totalCalories ?? 0);
    public readonly caloriesBurned = computed(() => this.snapshot()?.caloriesBurned ?? 0);
    public readonly meals = computed<Meal[]>(() =>
        (this.snapshot()?.meals.items ?? []).map(meal => ({ ...meal, ...this.favoriteStates()[meal.id] })),
    );
    public readonly latestWeight = computed(() => this.snapshot()?.weight.latest?.weightKg ?? null);
    public readonly previousWeight = computed(() => this.snapshot()?.weight.previous?.weightKg ?? null);
    public readonly desiredWeightKg = computed(() => this.snapshot()?.weight.desiredWeightKg ?? null);
    public readonly latestWaist = computed(() => this.snapshot()?.waist.latest?.circumferenceCm ?? null);
    public readonly previousWaist = computed(() => this.snapshot()?.waist.previous?.circumferenceCm ?? null);
    public readonly desiredWaistCm = computed(() => this.snapshot()?.waist.desiredWaistCm ?? null);
    public readonly weeklyConsumed = computed(() =>
        (this.snapshot()?.weeklyCalories ?? []).reduce((sum, point) => sum + point.calories, 0),
    );
    public readonly weeklyCalories = computed(() => this.snapshot()?.weeklyCalories ?? []);
    public readonly nutritionInsight = computed(() => resolveDashboardNutritionInsight(this.snapshot()));
    public readonly hydration = computed(() => this.snapshot()?.hydration ?? null);
    public readonly dailyAdvice = computed(() => this.snapshot()?.advice ?? null);
    public readonly currentFastingSession = computed<FastingSession | null>(() => this.snapshot()?.currentFastingSession ?? null);
    private readonly weightTrendPoints = computed(() => this.snapshot()?.weightTrend ?? []);
    private readonly waistTrendPoints = computed(() => this.snapshot()?.waistTrend ?? []);
    public readonly isHydrationLoading = computed(() => this.isLoading() || this.isHydrationUpdating() || this.isHydrationRefreshing());
    public readonly isWeightTrendLoading = computed(() => this.isLoading());
    public readonly isWaistTrendLoading = computed(() => this.isLoading());
    public readonly isAdviceLoading = computed(() => this.isLoading());

    public readonly weightTrend = createWeightTrendSignals(this.weightTrendPoints, this.latestWeight);
    public readonly waistTrend = createWaistTrendSignals(this.waistTrendPoints, this.latestWaist);
    public readonly nutrientBars = createNutrientBarsSignal(this.snapshot);
    public readonly mealRingData = createMealRingSignal(this.snapshot, this.weeklyConsumed, this.nutrientBars);
    public readonly mealPreviewEntries = createMealPreviewSignal(this.meals, this.isTodaySelected);
    public readonly fastingIsActive = computed(() => {
        const session = this.currentFastingSession();
        return session !== null && session.endedAtUtc === null;
    });
    public readonly placeholderIcon = placeholderIcon;
    public readonly placeholderLabel = placeholderLabel;

    public constructor() {
        this.localDay.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (this.followsToday && this.initialized()) {
                this.setSelectedDate();
            }
        });
    }

    public initialize(date?: Date): void {
        if (this.initialized()) {
            return;
        }

        this.followsToday = date === undefined;
        this.selectedDate.set(normalizeDate(date ?? this.localDay.refresh()));
        this.initialized.set(true);
        this.loadDashboardSnapshot();

        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.loadDashboardSnapshot(false);
        });
    }

    public setSelectedDate(date?: Date): void {
        this.followsToday = date === undefined;
        const normalized = normalizeDate(date ?? this.localDay.refresh());
        if (normalized.getTime() === this.selectedDate().getTime()) {
            return;
        }

        this.selectedDate.set(normalized);
        this.loadDashboardSnapshot();
    }

    public addHydration(amount: number): void {
        if (this.isHydrationLoading()) {
            return;
        }
        this.localDay.refresh();
        const targetDate = getHydrationDateUtc(this.selectedDate());
        runTrackedRequest(this.destroyRef, this.isHydrationUpdating, this.hydrationService.addEntry(amount, targetDate), {
            next: () => {
                this.loadDashboardSnapshot(false, true);
            },
            error: () => this.toastService.error(this.translateService.instant('HYDRATION_CARD.ADD_ERROR')),
        });
    }

    public applyTdeeGoal(target: number): void {
        if (this.isApplyingTdeeGoal()) {
            return;
        }
        void this.applyTdeeGoalAsync(target).then(saved => {
            if (!saved) {
                this.toastService.error(this.translateService.instant('TDEE_CARD.APPLY_ERROR'));
            }
        });
    }

    public async applyTdeeGoalAsync(target: number): Promise<boolean> {
        if (this.tdeeGoalRequest !== null) {
            return this.tdeeGoalRequest;
        }
        if (!Number.isFinite(target) || target <= 0) {
            return false;
        }
        this.isApplyingTdeeGoal.set(true);
        this.tdeeGoalRequest = this.persistTdeeGoalAsync(target).finally(() => {
            this.tdeeGoalRequest = null;
            this.isApplyingTdeeGoal.set(false);
        });
        return this.tdeeGoalRequest;
    }

    private async persistTdeeGoalAsync(target: number): Promise<boolean> {
        try {
            const goals = await firstValueFrom(
                this.goalsService.updateGoals({ dailyCalorieTarget: target }).pipe(takeUntilDestroyed(this.destroyRef)),
                { defaultValue: null },
            );
            if (goals === null) {
                return false;
            }
            this.loadDashboardSnapshot(false);
            return true;
        } catch {
            return false;
        }
    }

    public async openTdeeDetailsAsync(): Promise<TdeeInsightDialogAction | undefined> {
        const { TdeeInsightDialogComponent } = await import('../dialogs/tdee-insight-dialog/tdee-insight-dialog');
        return firstValueFrom(
            this.dialogService
                .open<TdeeInsightDialogComponentType, TdeeInsightDialogData, TdeeInsightDialogAction | undefined>(
                    TdeeInsightDialogComponent,
                    { size: 'md', data: { insight: this.tdeeInsight(), applyGoalAsync: this.applyTdeeGoalAsync.bind(this) } },
                )
                .afterClosed(),
        );
    }

    public reload(showLoader = true): void {
        this.loadDashboardSnapshot(showLoader);
    }

    private loadDashboardSnapshot(showLoader = true, refreshHydration = false): void {
        if (refreshHydration) {
            this.isHydrationRefreshing.set(true);
        }
        const requestId = this.snapshotRequest.begin({ showLoading: showLoader });
        const selectedDate = this.selectedDate();
        const targetDate = getDashboardDateUtc(selectedDate);
        const locale = this.getCurrentLocale();
        const query = {
            date: targetDate,
            timeZoneOffsetMinutes: -selectedDate.getTimezoneOffset(),
            timeZoneId: new Intl.DateTimeFormat().resolvedOptions().timeZone,
            page: 1,
            pageSize: 10,
            locale,
            trendDays: this.trendDays,
        };
        const request$ = showLoader ? this.dashboardService.getSnapshot(query) : this.dashboardService.getSnapshotSilentlyStrict(query);
        const observer: PartialObserver<DashboardSnapshot | null> = {
            next: snapshot => {
                if (snapshot === null) {
                    const failed = this.snapshotRequest.fail(requestId, 'DASHBOARD.LOAD_ERROR', { preserveData: !showLoader });
                    if (showLoader && failed) {
                        this.layout.initializeLayout(null);
                    }
                    return;
                }
                if (!this.snapshotRequest.succeed(requestId, snapshot)) {
                    return;
                }
                this.layout.initializeLayout(snapshot.dashboardLayout ?? null);
            },
            error: () => {
                if (!this.snapshotRequest.fail(requestId, 'DASHBOARD.LOAD_ERROR', { preserveData: !showLoader })) {
                    return;
                }

                if (showLoader) {
                    this.layout.initializeLayout(null);
                }
            },
        };

        request$
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    if (refreshHydration) {
                        this.isHydrationRefreshing.set(false);
                    }
                }),
            )
            .subscribe(observer);
    }

    private getCurrentLocale(): string {
        return resolveTranslateLanguage(this.translateService).split(/[_-]/)[0];
    }
}
