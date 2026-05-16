import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { GoalsService } from '../../goals/api/goals.service';
import { HydrationService } from '../../hydration/api/hydration.service';
import { DashboardService } from '../api/dashboard.service';
import type { DashboardSnapshot } from '../models/dashboard.data';
import { DashboardFacade } from './dashboard.facade';
import { DashboardLayoutService } from './dashboard-layout.service';

const HYDRATION_AMOUNT_ML = 250;
const TDEE_TARGET = 2300;

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

    it('should use normalized fallback locale for snapshot requests', () => {
        const { facade, dashboardService, translateService } = setupFacade();
        translateService.getCurrentLang.mockReturnValue('');
        translateService.getFallbackLang.mockReturnValue('ru-RU');

        facade.initialize();

        expect(dashboardService.getSnapshot).toHaveBeenCalledWith(expect.objectContaining({ locale: 'ru' }));
    });
});

describe('DashboardFacade actions', () => {
    it('should reload snapshot after hydration update succeeds', () => {
        const { facade, dashboardService, hydrationService } = setupFacade();
        facade.initialize();
        const initialCallCount = dashboardService.getSnapshot.mock.calls.length;

        facade.addHydration(HYDRATION_AMOUNT_ML);

        expect(hydrationService.addEntry).toHaveBeenCalled();
        expect(dashboardService.getSnapshot.mock.calls.length).toBe(initialCallCount + 1);
    });

    it('should update calorie goal and reload snapshot after applying TDEE suggestion', () => {
        const { facade, dashboardService, goalsService } = setupFacade();
        facade.initialize();
        const initialCallCount = dashboardService.getSnapshot.mock.calls.length;

        facade.applyTdeeGoal(TDEE_TARGET);

        expect(goalsService.updateGoals).toHaveBeenCalledWith({ dailyCalorieTarget: TDEE_TARGET });
        expect(dashboardService.getSnapshot.mock.calls.length).toBe(initialCallCount + 1);
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
    dashboardService: { getSnapshot: ReturnType<typeof vi.fn> };
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
    const dashboardService = { getSnapshot: vi.fn(() => of(snapshot)) };
    const goalsService = { updateGoals: vi.fn(() => of(undefined)) };
    const hydrationService = { addEntry: vi.fn(() => of(undefined)) };
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
            { provide: DashboardService, useValue: dashboardService },
            { provide: GoalsService, useValue: goalsService },
            { provide: HydrationService, useValue: hydrationService },
            { provide: DashboardLayoutService, useValue: layout },
            { provide: TranslateService, useValue: translateService },
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

function createSnapshot(): DashboardSnapshot {
    return {
        date: '2026-03-15',
        dailyGoal: 2100,
        weeklyCalorieGoal: 14700,
        statistics: {
            totalCalories: 1200,
            averageProteins: 90,
            averageFats: 45,
            averageCarbs: 140,
            averageFiber: 20,
        },
        meals: { items: [], total: 0 },
        hydration: { dateUtc: '2026-03-15T00:00:00.000Z', totalMl: 500, goalMl: 2000 },
        weeklyCalories: [],
        weight: { latest: null, previous: null, desired: null },
        waist: { latest: null, previous: null, desired: null },
        weightTrend: [],
        waistTrend: [],
        advice: null,
        dashboardLayout: { web: ['summary'], mobile: ['summary'] },
    };
}
