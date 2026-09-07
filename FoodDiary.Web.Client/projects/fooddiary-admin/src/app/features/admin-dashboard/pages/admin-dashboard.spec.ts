import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminDashboardFacade } from '../lib/admin-dashboard.facade';
import type { AdminDashboardOverview } from '../models/admin-dashboard-overview.data';

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
