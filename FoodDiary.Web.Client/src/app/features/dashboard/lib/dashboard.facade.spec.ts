import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HydrationService } from '../../hydration/api/hydration.service';
import { DashboardService } from '../api/dashboard.service';
import type { DashboardSnapshot } from '../models/dashboard.data';
import { DashboardFacade } from './dashboard.facade';
import { DashboardLayoutService } from './dashboard-layout.service';

const HYDRATION_AMOUNT_ML = 250;

describe('DashboardFacade', () => {
    let facade: DashboardFacade;
    let dashboardService: { getSnapshot: ReturnType<typeof vi.fn> };
    let hydrationService: { addEntry: ReturnType<typeof vi.fn> };
    let layout: { initializeLayout: ReturnType<typeof vi.fn>; updateViewportWidth: ReturnType<typeof vi.fn> };
    let translateService: {
        getCurrentLang: ReturnType<typeof vi.fn>;
        getFallbackLang: ReturnType<typeof vi.fn>;
        onLangChange: Subject<unknown>;
    };

    const snapshot: DashboardSnapshot = {
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

    beforeEach(() => {
        dashboardService = { getSnapshot: vi.fn() };
        hydrationService = { addEntry: vi.fn() };
        layout = { initializeLayout: vi.fn(), updateViewportWidth: vi.fn() };
        translateService = {
            getCurrentLang: vi.fn(() => 'en'),
            getFallbackLang: vi.fn(() => 'en'),
            onLangChange: new Subject(),
        };

        dashboardService.getSnapshot.mockReturnValue(of(snapshot));
        hydrationService.addEntry.mockReturnValue(of(undefined));

        TestBed.configureTestingModule({
            providers: [
                DashboardFacade,
                { provide: DashboardService, useValue: dashboardService },
                { provide: HydrationService, useValue: hydrationService },
                { provide: DashboardLayoutService, useValue: layout },
                { provide: TranslateService, useValue: translateService },
            ],
        });

        facade = TestBed.inject(DashboardFacade);
    });

    it('should load snapshot on initialize', () => {
        facade.initialize();

        expect(dashboardService.getSnapshot).toHaveBeenCalledTimes(1);
        expect(facade.snapshot()).toEqual(snapshot);
        expect(layout.initializeLayout).toHaveBeenCalledWith(snapshot.dashboardLayout);
    });

    it('should reload snapshot when selected date changes', () => {
        facade.initialize();
        const initialCallCount = dashboardService.getSnapshot.mock.calls.length;

        facade.setSelectedDate(new Date('2026-03-20T12:00:00Z'));

        expect(dashboardService.getSnapshot.mock.calls.length).toBe(initialCallCount + 1);
    });

    it('should reload snapshot after hydration update succeeds', () => {
        facade.initialize();
        const initialCallCount = dashboardService.getSnapshot.mock.calls.length;

        facade.addHydration(HYDRATION_AMOUNT_ML);

        expect(hydrationService.addEntry).toHaveBeenCalled();
        expect(dashboardService.getSnapshot.mock.calls.length).toBe(initialCallCount + 1);
    });

    it('should stop hydration loading when hydration update fails', () => {
        facade.initialize();
        hydrationService.addEntry.mockReturnValueOnce(throwError(() => new Error('hydration failed')));

        facade.addHydration(HYDRATION_AMOUNT_ML);

        expect(facade.isHydrationLoading()).toBe(false);
    });
});
