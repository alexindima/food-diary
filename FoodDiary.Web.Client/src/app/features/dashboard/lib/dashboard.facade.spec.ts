import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { DashboardFacade } from './dashboard.facade';
import { DashboardService } from '../api/dashboard.service';
import { HydrationService } from '../../hydration/api/hydration.service';
import { CyclesService } from '../../cycle-tracking/api/cycles.service';
import { DashboardLayoutService } from './dashboard-layout.service';

describe('DashboardFacade', () => {
    let facade: DashboardFacade;
    let dashboardService: { getSnapshot: ReturnType<typeof vi.fn> };
    let hydrationService: { addEntry: ReturnType<typeof vi.fn> };
    let cyclesService: { getCurrent: ReturnType<typeof vi.fn> };
    let layout: { initializeLayout: ReturnType<typeof vi.fn>; updateViewportWidth: ReturnType<typeof vi.fn> };
    let translateService: { currentLang: string; getDefaultLang: ReturnType<typeof vi.fn>; onLangChange: Subject<unknown> };

    const snapshot = {
        dailyGoal: 2100,
        statistics: { totalCalories: 1200 },
        meals: { items: [] },
        hydration: { totalMl: 500, goalMl: 2000 },
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
        cyclesService = { getCurrent: vi.fn() };
        layout = { initializeLayout: vi.fn(), updateViewportWidth: vi.fn() };
        translateService = {
            currentLang: 'en',
            getDefaultLang: vi.fn(() => 'en'),
            onLangChange: new Subject(),
        };

        dashboardService.getSnapshot.mockReturnValue(of(snapshot as any));
        hydrationService.addEntry.mockReturnValue(of(undefined));
        cyclesService.getCurrent.mockReturnValue(of(null));

        TestBed.configureTestingModule({
            providers: [
                DashboardFacade,
                { provide: DashboardService, useValue: dashboardService },
                { provide: HydrationService, useValue: hydrationService },
                { provide: CyclesService, useValue: cyclesService },
                { provide: DashboardLayoutService, useValue: layout },
                { provide: TranslateService, useValue: translateService },
            ],
        });

        facade = TestBed.inject(DashboardFacade);
    });

    it('should load snapshot and cycle on initialize', () => {
        facade.initialize();

        expect(dashboardService.getSnapshot).toHaveBeenCalledTimes(1);
        expect(cyclesService.getCurrent).toHaveBeenCalledTimes(1);
        expect(facade.snapshot()).toEqual(snapshot as any);
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

        facade.addHydration(250);

        expect(hydrationService.addEntry).toHaveBeenCalled();
        expect(dashboardService.getSnapshot.mock.calls.length).toBe(initialCallCount + 1);
    });

    it('should stop hydration loading when hydration update fails', () => {
        facade.initialize();
        hydrationService.addEntry.mockReturnValueOnce(throwError(() => new Error('hydration failed')));

        facade.addHydration(250);

        expect(facade.isHydrationLoading()).toBe(false);
    });
});
