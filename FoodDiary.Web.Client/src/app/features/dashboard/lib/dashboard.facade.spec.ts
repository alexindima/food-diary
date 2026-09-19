import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { GoalsService } from '../../goals/api/goals.service';
import { HydrationService } from '../../hydration/api/hydration.service';
import { FavoriteMealService } from '../../meals/api/favorite-meal.service';
import { MealService } from '../../meals/api/meal.service';
import type { FavoriteMeal, Meal } from '../../meals/models/meal.data';
import { DashboardService } from '../api/dashboard.service';
import type { DashboardSnapshot } from '../models/dashboard.data';
import { DashboardFacade } from './dashboard.facade';
import { DashboardLayoutService } from './dashboard-layout.service';

const UPDATED_HYDRATION_ML = 750;
const TEST_YEAR = 2026;
const SELECTED_DAY = 10;
const EVENING_HOUR = 19;
const NEXT_DAY = 11;
const CUSTOM_REDUCE_HOURS = 8;
const OTHER_DAY = 20;
const LATEST_WEIGHT = 78;
const PREVIOUS_WEIGHT = 79;
const DESIRED_WEIGHT = 75;
const LATEST_WAIST = 85;
const PREVIOUS_WAIST = 86;
const DESIRED_WAIST = 80;
const DAILY_CALORIE_GOAL = 2100;
const BURNED_CALORIES = 300;
const WEEKLY_CALORIES = 2800;

const HYDRATION_AMOUNT_ML = 250;
const SECOND_HYDRATION_AMOUNT_ML = 150;
const TDEE_TARGET = 2300;
const DEFAULT_SNAPSHOT_CALORIES = 1200;
const UPDATED_SNAPSHOT_CALORIES = 1800;

describe('Dashboard meal details', () => {
    it('opens the detail dialog and navigates only when Edit is selected', async () => {
        const { facade, snapshot } = setupFacade();
        const meal = {
            id: 'meal-details',
            date: '2026-03-15',
            totalCalories: 100,
            totalProteins: 0,
            totalFats: 0,
            totalCarbs: 0,
            totalFiber: 0,
            totalAlcohol: 0,
            isNutritionAutoCalculated: true,
            items: [],
        };
        snapshot.meals.items.push(meal);
        facade.initialize();
        const dialog = TestBed.inject(FdUiDialogService);
        const open = vi.spyOn(dialog, 'open');
        const closedDialog = { afterClosed: (): Observable<undefined> => of(undefined) };
        open.mockReturnValue(closedDialog as ReturnType<FdUiDialogService['open']>);
        const navigate = vi.spyOn(TestBed.inject(NavigationService), 'navigateToMealEditAsync');
        await facade.openMealDetailsAsync(meal.id);
        expect(open).toHaveBeenCalledWith(expect.any(Function), expect.objectContaining({ preset: 'detail', data: meal }));
        expect(navigate).not.toHaveBeenCalled();
        const editDialog = { afterClosed: (): Observable<{ action: string; id: string }> => of({ action: 'Edit', id: meal.id }) };
        open.mockReturnValue(editDialog as ReturnType<FdUiDialogService['open']>);
        await facade.openMealDetailsAsync(meal.id);
        expect(navigate).toHaveBeenCalledWith(meal.id);
    });
});
describe('DashboardFacade favorites', () => {
    it('toggles a dashboard meal favorite and ignores duplicate clicks while saving', () => {
        const { facade, snapshot } = setupFacade();
        snapshot.meals.items.push({
            id: 'meal-1',
            date: '2026-03-15',
            totalCalories: 100,
            totalProteins: 0,
            totalFats: 0,
            totalCarbs: 0,
            totalFiber: 0,
            totalAlcohol: 0,
            isNutritionAutoCalculated: true,
            items: [],
        });
        facade.initialize();
        const service = TestBed.inject(FavoriteMealService);
        const pending = new Subject<FavoriteMeal>();
        const add = vi.spyOn(service, 'add').mockReturnValue(pending);
        const remove = vi.spyOn(service, 'remove').mockReturnValue(of(undefined));
        facade.toggleMealFavorite('meal-1');
        facade.toggleMealFavorite('meal-1');
        expect(add).toHaveBeenCalledTimes(1);
        expect(facade.favoriteLoadingIds().has('meal-1')).toBe(true);
        pending.next({
            id: 'favorite-1',
            mealId: 'meal-1',
            name: null,
            createdAtUtc: '2026-03-15',
            mealDate: '2026-03-15',
            mealType: null,
            totalCalories: 100,
            totalProteins: 0,
            totalFats: 0,
            totalCarbs: 0,
            itemCount: 0,
        });
        pending.complete();
        expect(facade.meals()[0].isFavorite).toBe(true);
        expect(facade.favoriteLoadingIds().size).toBe(0);
        facade.toggleMealFavorite('meal-1');
        expect(remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.meals()[0].isFavorite).toBe(false);
    });
});

describe('DashboardFacade loading (1)', () => {
    it('should load snapshot on initialize', () => {
        const { facade, dashboardService, layout, snapshot } = setupFacade();

        facade.initialize();

        expect(dashboardService.getSnapshot).toHaveBeenCalledTimes(1);
        expect(facade.snapshot()).toEqual(snapshot);
        expect(layout.initializeLayout).toHaveBeenCalledWith(snapshot.dashboardLayout);
    });
    it('should reload snapshot when selected date changes', () => {
        const { facade, dashboardService } = setupFacade();
        facade.initialize();
        const initialCallCount = dashboardService.getSnapshot.mock.calls.length;

        facade.setSelectedDate(new Date('2026-03-20T12:00:00Z'));

        expect(dashboardService.getSnapshot.mock.calls.length).toBe(initialCallCount + 1);
    });
});

describe('DashboardFacade loading (2)', () => {
    it('should retain the current snapshot while a selected date is loading', () => {
        const { facade, dashboardService, snapshot } = setupFacade();
        const reload$ = new Subject<DashboardSnapshot>();
        facade.initialize();
        dashboardService.getSnapshot.mockReturnValueOnce(reload$);

        facade.setSelectedDate(new Date('2026-03-20T12:00:00Z'));

        expect(facade.isLoading()).toBe(true);
        expect(facade.hasSnapshot()).toBe(true);
        expect(facade.snapshot()).toEqual(snapshot);

        reload$.next(createSnapshot(UPDATED_SNAPSHOT_CALORIES));
        reload$.complete();
        expect(facade.isLoading()).toBe(false);
    });
    it('should use normalized fallback locale for snapshot requests', () => {
        const { facade, dashboardService, translateService } = setupFacade();
        translateService.getCurrentLang.mockReturnValue('');
        translateService.getFallbackLang.mockReturnValue('ru-RU');

        facade.initialize();

        expect(dashboardService.getSnapshot).toHaveBeenCalledWith(expect.objectContaining({ locale: 'ru' }));
    });
});

describe('DashboardFacade loading (3)', () => {
    it('should reload silently without toggling full dashboard loading', () => {
        const { facade, dashboardService, snapshot } = setupFacade();
        const reload$ = new Subject<DashboardSnapshot>();
        facade.initialize();
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(reload$);

        facade.reload(false);

        expect(facade.isLoading()).toBe(false);

        reload$.next(snapshot);
        reload$.complete();
        expect(facade.snapshot()).toEqual(snapshot);
    });
    it('should keep current snapshot when silent reload fails', () => {
        const { facade, dashboardService, snapshot, layout } = setupFacade();
        facade.initialize();
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(throwError(() => new Error('snapshot failed')));

        facade.reload(false);

        expect(facade.snapshot()).toEqual(snapshot);
        expect(layout.initializeLayout).toHaveBeenCalledTimes(1);
    });
});

describe('DashboardFacade loading (4)', () => {
    it('should record an error and preserve current data when silent reload returns null', () => {
        const { facade, dashboardService, snapshot, layout } = setupFacade();
        facade.initialize();
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(of(null));

        facade.reload(false);

        expect(facade.loadError()).toBe('DASHBOARD.LOAD_ERROR');
        expect(facade.snapshot()).toEqual(snapshot);
        expect(layout.initializeLayout).toHaveBeenCalledTimes(1);
    });
    it('should ignore stale snapshot responses after selected date changes', () => {
        const { facade, dashboardService } = setupFacade();
        const firstRequest$ = new Subject<DashboardSnapshot>();
        const secondRequest$ = new Subject<DashboardSnapshot>();
        const firstSnapshot = createSnapshot(DEFAULT_SNAPSHOT_CALORIES);
        const secondSnapshot = createSnapshot(UPDATED_SNAPSHOT_CALORIES);
        dashboardService.getSnapshot.mockReturnValueOnce(firstRequest$).mockReturnValueOnce(secondRequest$);

        facade.initialize();
        facade.setSelectedDate(new Date('2026-03-20T12:00:00Z'));
        secondRequest$.next(secondSnapshot);
        secondRequest$.complete();
        firstRequest$.next(firstSnapshot);
        firstRequest$.complete();

        expect(facade.snapshot()).toEqual(secondSnapshot);
        expect(facade.todayCalories()).toBe(UPDATED_SNAPSHOT_CALORIES);
    });
});

describe('DashboardFacade actions', () => {
    it('keeps water controls busy until the refreshed total arrives', () => {
        const { facade, hydrationService, dashboardService, snapshot } = setupFacade();
        const write = new Subject<void>();
        const refresh = new Subject<DashboardSnapshot>();
        hydrationService.addEntry.mockReturnValueOnce(write);
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(refresh);
        facade.initialize();
        facade.addHydration(HYDRATION_AMOUNT_ML);
        expect(facade.isHydrationLoading()).toBe(true);
        facade.addHydration(HYDRATION_AMOUNT_ML);
        expect(hydrationService.addEntry).toHaveBeenCalledTimes(1);
        write.next();
        write.complete();
        expect(facade.isHydrationLoading()).toBe(true);
        facade.addHydration(HYDRATION_AMOUNT_ML);
        expect(hydrationService.addEntry).toHaveBeenCalledTimes(1);
        refresh.next({ ...snapshot, hydration: { dateUtc: '2026-03-15T00:00:00.000Z', goalMl: 2000, totalMl: 750 } });
        refresh.complete();
        expect(facade.hydration()?.totalMl).toBe(UPDATED_HYDRATION_ML);
        expect(facade.isHydrationLoading()).toBe(false);
    });

    it('should reload snapshot after hydration update succeeds', () => {
        const { facade, dashboardService, hydrationService } = setupFacade();
        facade.initialize();

        facade.addHydration(HYDRATION_AMOUNT_ML);

        expect(hydrationService.addEntry).toHaveBeenCalled();
        expect(dashboardService.getSnapshotSilentlyStrict).toHaveBeenCalledTimes(1);
    });

    it('should send every sequential hydration action', () => {
        const { facade, hydrationService } = setupFacade();
        facade.initialize();

        facade.addHydration(HYDRATION_AMOUNT_ML);
        facade.addHydration(SECOND_HYDRATION_AMOUNT_ML);

        expect(hydrationService.addEntry).toHaveBeenCalledTimes(2);
        expect(hydrationService.addEntry).toHaveBeenNthCalledWith(1, HYDRATION_AMOUNT_ML, expect.any(Date));
        expect(hydrationService.addEntry).toHaveBeenNthCalledWith(2, SECOND_HYDRATION_AMOUNT_ML, expect.any(Date));
    });

    it('should update calorie goal and reload snapshot after applying TDEE suggestion', () => {
        const { facade, dashboardService, goalsService } = setupFacade();
        facade.initialize();

        facade.applyTdeeGoal(TDEE_TARGET);

        expect(goalsService.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: TDEE_TARGET });
        expect(dashboardService.getSnapshotSilentlyStrict).toHaveBeenCalledTimes(1);
    });

    it('should keep snapshot when hydration refresh fails after update succeeds', () => {
        const { facade, dashboardService, hydrationService, snapshot } = setupFacade();
        facade.initialize();
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(throwError(() => new Error('snapshot failed')));

        facade.addHydration(HYDRATION_AMOUNT_ML);

        expect(hydrationService.addEntry).toHaveBeenCalled();
        expect(facade.snapshot()).toEqual(snapshot);
        expect(facade.isHydrationLoading()).toBe(false);
    });

    it('should stop hydration loading when hydration update fails', () => {
        const { facade, hydrationService } = setupFacade();
        facade.initialize();
        hydrationService.addEntry.mockReturnValueOnce(throwError(() => new Error('hydration failed')));

        facade.addHydration(HYDRATION_AMOUNT_ML);

        expect(facade.isHydrationLoading()).toBe(false);
    });
});

describe('DashboardFacade request lifecycle', () => {
    it('initializes once with the requested local day and ignores a time change within that day', () => {
        const { facade, dashboardService } = setupFacade();
        const date = new Date(TEST_YEAR, 2, SELECTED_DAY, EVENING_HOUR);
        facade.initialize(date);
        facade.initialize(new Date(TEST_YEAR, 2, NEXT_DAY));
        facade.setSelectedDate(new Date(TEST_YEAR, 2, SELECTED_DAY, CUSTOM_REDUCE_HOURS));
        expect(facade.selectedDate()).toEqual(new Date(TEST_YEAR, 2, SELECTED_DAY));
        expect(facade.isTodaySelected()).toBe(false);
        expect(dashboardService.getSnapshot).toHaveBeenCalledTimes(1);
    });

    it('refreshes in the new language without clearing the current snapshot', () => {
        const { facade, dashboardService, translateService, snapshot } = setupFacade();
        facade.initialize();
        translateService.getCurrentLang.mockReturnValue('ru-RU');
        translateService.onLangChange.next({ lang: 'ru-RU' });
        expect(dashboardService.getSnapshotSilentlyStrict).toHaveBeenCalledWith(expect.objectContaining({ locale: 'ru' }));
        expect(facade.snapshot()).toEqual(snapshot);
        expect(facade.isLoading()).toBe(false);
    });

    it.each(['null', 'error'] as const)('finishes initial loading with an error for %s and recovers on retry', failure => {
        const { facade, dashboardService, layout, snapshot } = setupFacade();
        dashboardService.getSnapshot.mockReturnValueOnce(failure === 'null' ? of(null) : throwError(() => new Error('offline')));
        facade.initialize();
        expect(facade.loadError()).toBe('DASHBOARD.LOAD_ERROR');
        expect(facade.isLoading()).toBe(false);
        expect(facade.hasSnapshot()).toBe(false);
        expect(layout.initializeLayout).toHaveBeenCalledWith(null);
        facade.reload();
        expect(facade.loadError()).toBeNull();
        expect(facade.snapshot()).toEqual(snapshot);
    });

    it.each(['null', 'error'] as const)('ignores a stale %s after a newer day succeeds', failure => {
        const { facade, dashboardService, snapshot, layout } = setupFacade();
        const pending = new Subject<DashboardSnapshot | null>();
        dashboardService.getSnapshot.mockReturnValueOnce(pending);
        facade.initialize();
        facade.setSelectedDate(new Date(TEST_YEAR, 2, OTHER_DAY));
        if (failure === 'null') {
            pending.next(null);
        } else {
            pending.error(new Error('late error'));
        }
        expect(facade.snapshot()).toEqual(snapshot);
        expect(facade.loadError()).toBeNull();
        expect(layout.initializeLayout).toHaveBeenCalledTimes(1);
    });

    it('unsubscribes pending loads and language changes on destruction', () => {
        const { facade, dashboardService, translateService, layout } = setupFacade();
        const pending = new Subject<DashboardSnapshot>();
        dashboardService.getSnapshot.mockReturnValueOnce(pending);
        facade.initialize();
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        translateService.onLangChange.next({ lang: 'ru' });
        pending.next(createSnapshot());
        expect(dashboardService.getSnapshotSilentlyStrict).not.toHaveBeenCalled();
        expect(layout.initializeLayout).not.toHaveBeenCalled();
    });
});

describe('DashboardFacade meal mutations (1)', () => {
    it('ignores actions for a missing meal', async () => {
        const { facade } = setupFacade();
        await facade.openMealDetailsAsync('missing');
        facade.toggleMealFavorite('missing');
        expect(vi.spyOn(TestBed.inject(FdUiDialogService), 'open')).not.toHaveBeenCalled();
        expect(vi.spyOn(TestBed.inject(FavoriteMealService), 'add')).not.toHaveBeenCalled();
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });
    it.each(['Repeat', 'Delete'] as const)('invalidates nutrition and refreshes after %s succeeds', async action => {
        const { facade, snapshot, dashboardService } = setupFacade();
        snapshot.meals.items.push(createMeal());
        facade.initialize();
        mockMealDialog({ action, id: 'meal-1' });
        const meals = TestBed.inject(MealService);
        vi.mocked(vi.spyOn(meals, 'repeat')).mockReturnValue(of(createMeal()));
        vi.mocked(vi.spyOn(meals, 'deleteById')).mockReturnValue(of(undefined));
        await facade.openMealDetailsAsync('meal-1');
        if (action === 'Repeat') {
            expect(vi.spyOn(meals, 'repeat')).toHaveBeenCalledWith('meal-1', expect.any(String), expect.any(String));
            expect(vi.spyOn(meals, 'deleteById')).not.toHaveBeenCalled();
        } else {
            expect(vi.spyOn(meals, 'deleteById')).toHaveBeenCalledWith('meal-1');
            expect(vi.spyOn(meals, 'repeat')).not.toHaveBeenCalled();
        }
        expect(vi.spyOn(TestBed.inject(NutritionDataInvalidationService), 'reportMealMutation')).toHaveBeenCalledTimes(1);
        expect(dashboardService.getSnapshotSilentlyStrict).toHaveBeenCalledTimes(1);
    });
});

describe('DashboardFacade meal mutations (2)', () => {
    it.each(['Repeat', 'Delete'] as const)('reports %s failure without invalidating or refreshing', async action => {
        const { facade, snapshot, dashboardService } = setupFacade();
        snapshot.meals.items.push(createMeal());
        facade.initialize();
        mockMealDialog({ action, id: 'meal-1' });
        const meals = TestBed.inject(MealService);
        vi.mocked(vi.spyOn(meals, 'repeat')).mockReturnValue(throwError(() => new Error('offline')));
        vi.mocked(vi.spyOn(meals, 'deleteById')).mockReturnValue(throwError(() => new Error('offline')));
        await facade.openMealDetailsAsync('meal-1');
        expect(vi.spyOn(TestBed.inject(FdUiToastService), 'error')).toHaveBeenCalledWith('MEAL_LIST.OPERATION_ERROR_MESSAGE');
        expect(vi.spyOn(TestBed.inject(NutritionDataInvalidationService), 'reportMealMutation')).not.toHaveBeenCalled();
        expect(dashboardService.getSnapshotSilentlyStrict).not.toHaveBeenCalled();
    });
    it('clears local favorite overrides when the detail dialog changes favorites', async () => {
        const { facade, snapshot, dashboardService } = setupFacade();
        snapshot.meals.items.push(createMeal());
        facade.initialize();
        vi.spyOn(TestBed.inject(FavoriteMealService), 'add').mockReturnValue(of(createFavorite()));
        facade.toggleMealFavorite('meal-1');
        expect(facade.meals()[0].isFavorite).toBe(true);
        mockMealDialog({ action: 'FavoriteChanged', id: 'meal-1' });
        await facade.openMealDetailsAsync('meal-1');
        expect(facade.meals()[0].isFavorite).not.toBe(true);
        expect(dashboardService.getSnapshotSilentlyStrict).toHaveBeenCalledTimes(1);
        expect(vi.spyOn(TestBed.inject(MealService), 'deleteById')).not.toHaveBeenCalled();
    });
});

describe('DashboardFacade meal mutations (3)', () => {
    it.each([true, false])('looks up a missing favorite id (found=%s)', found => {
        const { facade, snapshot } = setupFacade();
        snapshot.meals.items.push({ ...createMeal(), isFavorite: true });
        facade.initialize();
        const favorites = TestBed.inject(FavoriteMealService);
        vi.mocked(vi.spyOn(favorites, 'getAll')).mockReturnValue(of(found ? [createFavorite()] : []));
        vi.mocked(vi.spyOn(favorites, 'remove')).mockReturnValue(of(undefined));
        facade.toggleMealFavorite('meal-1');
        expect(vi.spyOn(favorites, 'getAll')).toHaveBeenCalledTimes(1);
        if (found) {
            expect(vi.spyOn(favorites, 'remove')).toHaveBeenCalledWith('favorite-1');
        } else {
            expect(vi.spyOn(favorites, 'remove')).not.toHaveBeenCalled();
        }
        expect(facade.meals()[0].isFavorite).toBe(false);
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });
    it.each([false, true])('preserves favorite state and allows retry after an error (wasFavorite=%s)', wasFavorite => {
        const { facade, snapshot } = setupFacade();
        snapshot.meals.items.push({ ...createMeal(), isFavorite: wasFavorite, favoriteMealId: 'favorite-1' });
        facade.initialize();
        const favorites = TestBed.inject(FavoriteMealService);
        vi.mocked(vi.spyOn(favorites, 'add')).mockReturnValue(throwError(() => new Error('offline')));
        vi.mocked(vi.spyOn(favorites, 'remove')).mockReturnValue(throwError(() => new Error('offline')));
        facade.toggleMealFavorite('meal-1');
        expect(facade.meals()[0].isFavorite).toBe(wasFavorite);
        expect(facade.favoriteLoadingIds().size).toBe(0);
        expect(vi.spyOn(TestBed.inject(FdUiToastService), 'error')).toHaveBeenCalledWith('MEAL_LIST.OPERATION_ERROR_MESSAGE');
        vi.mocked(vi.spyOn(favorites, 'add')).mockReturnValue(of(createFavorite()));
        vi.mocked(vi.spyOn(favorites, 'remove')).mockReturnValue(of(undefined));
        facade.toggleMealFavorite('meal-1');
        expect(facade.meals()[0].isFavorite).toBe(!wasFavorite);
    });
});

describe('DashboardFacade meal mutations (4)', () => {
    it('cancels a pending favorite mutation and clears busy state on destruction', () => {
        const { facade, snapshot } = setupFacade();
        snapshot.meals.items.push(createMeal());
        facade.initialize();
        const pending = new Subject<FavoriteMeal>();
        vi.spyOn(TestBed.inject(FavoriteMealService), 'add').mockReturnValue(pending);
        facade.toggleMealFavorite('meal-1');
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(facade.favoriteLoadingIds().size).toBe(0);
        expect(facade.meals()[0].isFavorite).not.toBe(true);
    });
});

describe('DashboardFacade view data (1)', () => {
    it('provides safe empty values before the first snapshot and while loading', () => {
        const { facade, dashboardService } = setupFacade();
        expect(facade.isTodaySelected()).toBe(true);
        expect(facade.dailyGoal()).toBe(0);
        expect(facade.todayCalories()).toBe(0);
        expect(facade.caloriesBurned()).toBe(0);
        expect(facade.meals()).toEqual([]);
        expect(facade.weeklyCalories()).toEqual([]);
        expect(facade.weeklyConsumed()).toBe(0);
        for (const value of [
            facade.latestWeight(),
            facade.previousWeight(),
            facade.desiredWeightKg(),
            facade.latestWaist(),
            facade.previousWaist(),
            facade.desiredWaistCm(),
            facade.hydration(),
            facade.dailyAdvice(),
            facade.cycle(),
            facade.tdeeInsight(),
            facade.currentFastingSession(),
            facade.weightTrend.weightTrendCurrent(),
            facade.waistTrend.waistTrendCurrent(),
        ]) {
            expect(value).toBeNull();
        }
        expect(facade.fastingIsActive()).toBe(false);
        const pending = new Subject<DashboardSnapshot>();
        dashboardService.getSnapshot.mockReturnValueOnce(pending);
        facade.initialize();
        for (const loading of [
            facade.isCycleLoading(),
            facade.isAdviceLoading(),
            facade.isWeightTrendLoading(),
            facade.isWaistTrendLoading(),
            facade.isHydrationLoading(),
        ]) {
            expect(loading).toBe(true);
        }
        pending.next(createSnapshot());
        pending.complete();
        expect(facade.isCycleLoading()).toBe(false);
        expect(facade.isAdviceLoading()).toBe(false);
        expect(facade.isWeightTrendLoading()).toBe(false);
        expect(facade.isWaistTrendLoading()).toBe(false);
    });
});

describe('DashboardFacade snapshot mapping', () => {
    it('maps real snapshot measurements and weekly totals without mixing weight and waist', () => {
        const { facade, snapshot } = setupFacade();
        snapshot.weight = {
            latest: { date: '2026-03-15', weightKg: 78 },
            previous: { date: '2026-03-14', weightKg: 79 },
            desiredWeightKg: 75,
        };
        snapshot.waist = {
            latest: { date: '2026-03-15', circumferenceCm: 85 },
            previous: { date: '2026-03-14', circumferenceCm: 86 },
            desiredWaistCm: 80,
        };
        snapshot.caloriesBurned = 300;
        snapshot.weeklyCalories = [
            { date: '2026-03-14', calories: 1600 },
            { date: '2026-03-15', calories: 1200 },
        ];
        facade.initialize();
        expect([facade.latestWeight(), facade.previousWeight(), facade.desiredWeightKg()]).toEqual([
            LATEST_WEIGHT,
            PREVIOUS_WEIGHT,
            DESIRED_WEIGHT,
        ]);
        expect([facade.latestWaist(), facade.previousWaist(), facade.desiredWaistCm()]).toEqual([
            LATEST_WAIST,
            PREVIOUS_WAIST,
            DESIRED_WAIST,
        ]);
        expect(facade.weightTrend.weightTrendCurrent()).toBe(LATEST_WEIGHT);
        expect(facade.waistTrend.waistTrendCurrent()).toBe(LATEST_WAIST);
        expect(facade.dailyGoal()).toBe(DAILY_CALORIE_GOAL);
        expect(facade.caloriesBurned()).toBe(BURNED_CALORIES);
        expect(facade.weeklyConsumed()).toBe(WEEKLY_CALORIES);
        expect(facade.weeklyCalories()).toEqual(snapshot.weeklyCalories);
        expect(facade.hydration()).toEqual(snapshot.hydration);
        expect(facade.nutritionInsight()).toBeDefined();
    });
});

describe('DashboardFacade view data (2)', () => {
    it('opens TDEE details with the current snapshot insight', async () => {
        const { facade } = setupFacade();
        facade.initialize();
        const open = vi.spyOn(TestBed.inject(FdUiDialogService), 'open');
        const dialog = { afterClosed: (): Observable<undefined> => of(undefined) };
        open.mockReturnValue(dialog as ReturnType<FdUiDialogService['open']>);
        await expect(facade.openTdeeDetailsAsync()).resolves.toBeUndefined();
        expect(open).toHaveBeenCalledWith(expect.any(Function), { size: 'md', data: { insight: null } });
    });
    it('cancels a pending water write when the page is destroyed', () => {
        const { facade, hydrationService, dashboardService } = setupFacade();
        const pending = new Subject<void>();
        hydrationService.addEntry.mockReturnValueOnce(pending);
        facade.initialize();
        facade.addHydration(HYDRATION_AMOUNT_ML);
        expect(facade.isHydrationLoading()).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        expect(facade.isHydrationLoading()).toBe(false);
        expect(dashboardService.getSnapshotSilentlyStrict).not.toHaveBeenCalled();
    });
});

function createMeal(): Meal {
    return {
        id: 'meal-1',
        date: '2026-03-15',
        totalCalories: 100,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        items: [],
    };
}

function createFavorite(): FavoriteMeal {
    return {
        id: 'favorite-1',
        mealId: 'meal-1',
        name: null,
        createdAtUtc: '2026-03-15',
        mealDate: '2026-03-15',
        mealType: null,
        totalCalories: 100,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        itemCount: 0,
    };
}

function mockMealDialog(result: { action: string; id: string }): void {
    const dialog = { afterClosed: (): Observable<typeof result> => of(result) };
    vi.spyOn(TestBed.inject(FdUiDialogService), 'open').mockReturnValue(dialog as ReturnType<FdUiDialogService['open']>);
}

function setupFacade(): {
    facade: DashboardFacade;
    dashboardService: {
        getSnapshot: ReturnType<typeof vi.fn>;
        getSnapshotSilentlyStrict: ReturnType<typeof vi.fn>;
    };
    goalsService: { updateGoals: ReturnType<typeof vi.fn> };
    hydrationService: { addEntry: ReturnType<typeof vi.fn> };
    layout: { initializeLayout: ReturnType<typeof vi.fn>; updateViewportWidth: ReturnType<typeof vi.fn> };
    snapshot: DashboardSnapshot;
    translateService: {
        getCurrentLang: ReturnType<typeof vi.fn>;
        getFallbackLang: ReturnType<typeof vi.fn>;
        onLangChange: Subject<unknown>;
    };
} {
    const snapshot = createSnapshot();
    const dashboardService = {
        getSnapshot: vi.fn(() => of(snapshot)),
        getSnapshotSilentlyStrict: vi.fn(() => of(snapshot)),
    };
    const goalsService = { updateGoals: vi.fn(() => of(void 0)) };
    const hydrationService = { addEntry: vi.fn(() => of(void 0)) };
    const layout = { initializeLayout: vi.fn(), updateViewportWidth: vi.fn() };
    const translateService = {
        getCurrentLang: vi.fn(() => 'en'),
        getFallbackLang: vi.fn(() => 'en'),
        onLangChange: new Subject<unknown>(),
        instant: (key: string): string => key,
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [
            DashboardFacade,
            { provide: NavigationService, useValue: { navigateToMealEditAsync: vi.fn() } },
            { provide: NutritionDataInvalidationService, useValue: { reportMealMutation: vi.fn() } },
            { provide: MealService, useValue: { repeat: vi.fn(), deleteById: vi.fn() } },
            { provide: FavoriteMealService, useValue: { add: vi.fn(), remove: vi.fn(), getAll: vi.fn() } },
            { provide: FdUiToastService, useValue: { error: vi.fn() } },
            { provide: DashboardService, useValue: dashboardService },
            { provide: GoalsService, useValue: goalsService },
            { provide: HydrationService, useValue: hydrationService },
            { provide: DashboardLayoutService, useValue: layout },
            { provide: TranslateService, useValue: translateService },
            { provide: FdUiDialogService, useValue: { open: vi.fn() } },
        ],
    });

    return {
        facade: TestBed.inject(DashboardFacade),
        dashboardService,
        goalsService,
        hydrationService,
        layout,
        snapshot,
        translateService,
    };
}

function createSnapshot(totalCalories = DEFAULT_SNAPSHOT_CALORIES): DashboardSnapshot {
    return {
        date: '2026-03-15',
        dateTo: '2026-03-15',
        dailyGoal: 2100,
        weeklyCalorieGoal: 14700,
        statistics: {
            totalCalories,
            averageProteins: 90,
            averageFats: 45,
            averageCarbs: 140,
            averageFiber: 20,
        },
        meals: { items: [], total: 0 },
        hydration: { dateUtc: '2026-03-15T00:00:00.000Z', totalMl: 500, goalMl: 2000 },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desiredWeightKg: null },
        waist: { latest: null, previous: null, desiredWaistCm: null },
        weightTrend: [],
        waistTrend: [],
        advice: null,
        dashboardLayout: { web: ['summary'], mobile: ['summary'] },
    };
}

describe('Dashboard water refresh cancellation', () => {
    it.each(['success', 'error'] as const)('releases water controls after an obsolete refresh %s', outcome => {
        const { facade, dashboardService, snapshot } = setupFacade();
        const refresh = new Subject<DashboardSnapshot>();
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(refresh);
        facade.initialize();
        facade.addHydration(HYDRATION_AMOUNT_ML);
        facade.setSelectedDate(new Date(TEST_YEAR, 2, OTHER_DAY));
        expect(facade.isHydrationLoading()).toBe(true);
        if (outcome === 'success') {
            refresh.next(createSnapshot(UPDATED_SNAPSHOT_CALORIES));
            refresh.complete();
        } else {
            refresh.error(new Error('late failure'));
        }
        expect(facade.snapshot()).toEqual(snapshot);
        expect(facade.loadError()).toBeNull();
        expect(facade.isHydrationLoading()).toBe(false);
    });

    it('cancels the refresh as well as the write when the page is destroyed', () => {
        const { facade, dashboardService } = setupFacade();
        const refresh = new Subject<DashboardSnapshot>();
        dashboardService.getSnapshotSilentlyStrict.mockReturnValueOnce(refresh);
        facade.initialize();
        facade.addHydration(HYDRATION_AMOUNT_ML);
        expect(facade.isHydrationLoading()).toBe(true);
        TestBed.resetTestingModule();
        expect(refresh.observed).toBe(false);
        expect(facade.isHydrationLoading()).toBe(false);
    });
});
