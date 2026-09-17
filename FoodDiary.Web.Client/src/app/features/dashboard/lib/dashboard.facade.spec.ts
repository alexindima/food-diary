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
import type { FavoriteMeal } from '../../meals/models/meal.data';
import { DashboardService } from '../api/dashboard.service';
import type { DashboardSnapshot } from '../models/dashboard.data';
import { DashboardFacade } from './dashboard.facade';
import { DashboardLayoutService } from './dashboard-layout.service';

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

describe('DashboardFacade loading', () => {
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
