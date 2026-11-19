import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
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

function renderDashboard(getOverview = vi.fn().mockReturnValue(of(overview))): ComponentFixture<AdminDashboardComponent> {
    TestBed.configureTestingModule({
        imports: [AdminDashboardComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            AdminDashboardFacade,
            { provide: AdminDashboardService, useValue: { getOverview } },
        ],
    });
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    fixture.detectChanges();
    return fixture;
}

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

describe('Admin dashboard page states', () => {
    it('replaces stale counts with loading and an error, then renders the fresh result after a visible refresh', async () => {
        const pending = new Subject<AdminDashboardOverview>();
        const getOverview = vi.fn().mockReturnValueOnce(of(overview)).mockReturnValueOnce(pending).mockReturnValue(of(overview));
        const fixture = renderDashboard(getOverview);
        const host = fixture.nativeElement as HTMLElement;
        const content = 'fd-admin-dashboard-content';
        expect(host.querySelector(content)).not.toBeNull();

        await TestBed.inject(Router).navigateByUrl('/?period=all');
        fixture.detectChanges();
        expect(getOverview).toHaveBeenLastCalledWith({ allTime: true });
        expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_OVERVIEW.LOADING');
        expect(host.querySelector(content)).toBeNull();

        pending.error(new Error('report unavailable'));
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.ERROR');
        expect(host.querySelector(content)).toBeNull();

        const refresh = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === 'ADMIN_OVERVIEW.REFRESH');
        expect(refresh).toBeDefined();
        refresh?.click();
        fixture.detectChanges();
        expect(getOverview).toHaveBeenLastCalledWith({ allTime: true });
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(host.querySelector(content)).not.toBeNull();
    });

    it('clears the overview and ignores a pending request when a reversed custom range replaces it', async () => {
        const pending = new Subject<AdminDashboardOverview>();
        const getOverview = vi.fn().mockReturnValue(of(overview));
        const fixture = renderDashboard(getOverview);
        const router = TestBed.inject(Router);
        const host = fixture.nativeElement as HTMLElement;
        getOverview.mockReturnValueOnce(pending);
        await router.navigateByUrl('/?period=custom&from=2024-02-29&to=2024-02-29');
        fixture.detectChanges();
        expect(getOverview).toHaveBeenLastCalledWith({ from: '2024-02-29', to: '2024-02-29' });
        expect(host.querySelector('[role="status"]')).not.toBeNull();
        getOverview.mockClear();

        await router.navigateByUrl('/?period=custom&from=2024-03-01&to=2024-02-29');
        pending.next(overview);
        fixture.detectChanges();
        expect(getOverview).not.toHaveBeenCalled();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.INVALID');
        expect(host.querySelector('[role="status"]')).toBeNull();
        expect(host.querySelector('fd-admin-dashboard-content')).toBeNull();
    });
});
