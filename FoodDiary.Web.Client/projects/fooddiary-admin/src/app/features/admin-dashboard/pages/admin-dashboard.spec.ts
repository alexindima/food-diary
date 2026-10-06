import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminDashboardFacade } from '../lib/admin-dashboard.facade';
import type { AdminDashboardOverview } from '../models/admin-dashboard-overview.data';
import { AdminDashboardComponent } from './admin-dashboard';

const overview: AdminDashboardOverview = {
    fromUtc: '2026-09-01T00:00:00Z',
    toUtc: '2026-09-08T00:00:00Z',
    interval: 'day',
    period: {
        fromUtc: '2026-09-01T00:00:00Z',
        toUtc: '2026-09-08T00:00:00Z',
        metrics: { registrations: 4, payingUsers: 2, aiTokens: 800, trend: [] },
        currencies: [],
    },
    previous: null,
    totalUsersNow: 40,
    premiumUsersNow: 10,
    pendingReportsNow: 2,
};

function setup(): { facade: AdminDashboardFacade; getOverview: ReturnType<typeof vi.fn> } {
    const getOverview = vi.fn().mockReturnValue(of(overview));
    TestBed.configureTestingModule({ providers: [AdminDashboardFacade, { provide: AdminDashboardService, useValue: { getOverview } }] });
    return { facade: TestBed.inject(AdminDashboardFacade), getOverview };
}

describe('Admin dashboard overview', () => {
    it('loads the selected range and keeps current state separate', () => {
        const { facade, getOverview } = setup();
        facade.load({ from: '2026-09-01', to: '2026-09-07' });
        expect(getOverview).toHaveBeenCalledWith({ from: '2026-09-01', to: '2026-09-07' });
        expect(facade.overview()?.period.metrics.registrations).toBe(overview.period.metrics.registrations);
        expect(facade.overview()?.totalUsersNow).toBe(overview.totalUsersNow);
        expect(facade.isLoading()).toBe(false);
    });
    it('does not display failed requests as zero activity and supports retry', () => {
        const { facade, getOverview } = setup();
        getOverview.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
        facade.load();
        expect(facade.failed()).toBe(true);
        expect(facade.overview()).toBeNull();
        facade.load();
        expect(facade.failed()).toBe(false);
        expect(facade.overview()).toEqual(overview);
    });
    it('cancels stale responses after changing the period', () => {
        const { facade, getOverview } = setup();
        const old = new Subject<AdminDashboardOverview>();
        getOverview.mockReturnValueOnce(old);
        facade.load();
        facade.load({ allTime: true });
        old.next({ ...overview, totalUsersNow: 999 });
        expect(facade.overview()?.totalUsersNow).toBe(overview.totalUsersNow);
    });
});

describe('dashboard calendar validation', () => {
    it.each(['2026-02-31', '2026-04-31', '2025-02-29', '2026-13-01'])('blocks impossible calendar date %s before loading', date => {
        const load = vi.fn();
        const clear = vi.fn();
        TestBed.configureTestingModule({
            imports: [AdminDashboardComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                {
                    provide: AdminDashboardFacade,
                    useValue: { load, clear, overview: signal(null), isLoading: signal(false), failed: signal(false) },
                },
            ],
        });
        const fixture = TestBed.createComponent(AdminDashboardComponent);
        load.mockClear();
        fixture.componentInstance['preset'].set('custom');
        fixture.componentInstance['from'].set(date);
        fixture.componentInstance['to'].set('2026-10-01');
        fixture.componentInstance['reload']();
        fixture.detectChanges();
        expect(load).not.toHaveBeenCalled();
        expect(clear).toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.INVALID');
    });
});
