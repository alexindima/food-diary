import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminDashboardContentComponent } from '../components/admin-dashboard-content';
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

async function renderDashboardAsync(getOverview = vi.fn().mockReturnValue(of(overview)), path = '/'): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
        imports: [AdminDashboardComponent],
        providers: [
            provideRouter(
                [{ path: '', component: AdminDashboardComponent }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            AdminDashboardFacade,
            { provide: AdminDashboardService, useValue: { getOverview } },
        ],
    });
    const fixture = await RouterTestingHarness.create(path);
    await fixture.fixture.whenStable();
    return fixture;
}

function renderedOverview(fixture: RouterTestingHarness): AdminDashboardOverview | undefined {
    return fixture.routeDebugElement
        ?.queryAll(By.directive(AdminDashboardContentComponent))
        .at(0)
        ?.injector.get(AdminDashboardContentComponent)
        .data();
}

function refreshDashboard(fixture: RouterTestingHarness): void {
    const host = fixture.routeNativeElement as HTMLElement;
    const refresh = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === 'ADMIN_OVERVIEW.REFRESH');
    expect(refresh).toBeDefined();
    refresh?.click();
    fixture.detectChanges();
}

describe('Admin dashboard overview', () => {
    it('loads the selected range and keeps current state separate', async () => {
        const getOverview = vi.fn().mockReturnValue(of(overview));
        const fixture = await renderDashboardAsync(getOverview, '/?period=custom&from=2024-02-01&to=2024-02-07');
        expect(getOverview).toHaveBeenCalledExactlyOnceWith({ from: '2024-02-01', to: '2024-02-07' });
        expect(renderedOverview(fixture)?.period.metrics.registrations).toBe(overview.period.metrics.registrations);
        expect(renderedOverview(fixture)?.totalUsersNow).toBe(overview.totalUsersNow);
        expect(fixture.routeNativeElement?.querySelector('[role="status"]')).toBeNull();
    });
    it('does not display failed requests as zero activity and supports retry', async () => {
        const getOverview = vi
            .fn()
            .mockReturnValueOnce(throwError(() => new Error('Unavailable')))
            .mockReturnValue(of(overview));
        const fixture = await renderDashboardAsync(getOverview, '/?period=all');
        expect(fixture.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.ERROR');
        expect(renderedOverview(fixture)).toBeUndefined();

        refreshDashboard(fixture);
        await fixture.fixture.whenStable();
        expect(fixture.routeNativeElement?.querySelector('[role="alert"]')).toBeNull();
        expect(renderedOverview(fixture)).toEqual(overview);
    });
    it.each([
        { action: 'changing the period', nextRange: { allTime: true } },
        { action: 'refreshing the same period', nextRange: undefined },
    ])('cancels stale responses when $action', async ({ nextRange }) => {
        const getOverview = vi.fn().mockReturnValue(of(overview));
        const fixture = await renderDashboardAsync(getOverview);
        const router = TestBed.inject(Router);
        const old = new Subject<AdminDashboardOverview>();
        const range = { from: '2024-02-01', to: '2024-02-07' };
        getOverview.mockClear();
        getOverview.mockReturnValueOnce(old);
        await router.navigateByUrl(`/?period=custom&from=${range.from}&to=${range.to}`);
        fixture.detectChanges();
        expect(old.observed).toBe(true);

        if (nextRange === undefined) {
            refreshDashboard(fixture);
        } else {
            await router.navigateByUrl('/?period=all');
            fixture.detectChanges();
        }
        expect(old.observed).toBe(false);
        expect(getOverview).toHaveBeenCalledTimes(2);
        expect(getOverview).toHaveBeenLastCalledWith(nextRange ?? range);
        old.next({ ...overview, totalUsersNow: 999 });
        await fixture.fixture.whenStable();
        expect(renderedOverview(fixture)?.totalUsersNow).toBe(overview.totalUsersNow);
    });
});

describe('dashboard calendar validation', () => {
    it.each([
        ['2026-02-31', '2026-10-01'],
        ['2026-04-31', '2026-10-01'],
        ['2025-02-29', '2026-10-01'],
        ['2026-13-01', '2026-10-01'],
        ['1969-12-31', '1970-01-01'],
        ['2024-01-01', '2999-01-01'],
        ['2000-01-01', '2011-01-01'],
    ])('rejects out-of-bounds URL range %s to %s without issuing a report request', async (from, to) => {
        const getOverview = vi.fn().mockReturnValue(of(overview));
        const fixture = await renderDashboardAsync(getOverview);
        getOverview.mockClear();
        await TestBed.inject(Router).navigateByUrl(`/?period=custom&from=${from}&to=${to}`);
        fixture.detectChanges();
        const host = fixture.routeNativeElement as HTMLElement;
        expect(getOverview).not.toHaveBeenCalled();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.INVALID');
        expect(host.querySelector('fd-admin-dashboard-content')).toBeNull();
    });
});

describe('Admin dashboard page states', () => {
    it('replaces stale counts with loading and an error, then renders the fresh result after a visible refresh', async () => {
        const pending = new Subject<AdminDashboardOverview>();
        const getOverview = vi.fn().mockReturnValueOnce(of(overview)).mockReturnValueOnce(pending).mockReturnValue(of(overview));
        const fixture = await renderDashboardAsync(getOverview);
        const host = fixture.routeNativeElement as HTMLElement;
        const content = 'fd-admin-dashboard-content';
        expect(host.querySelector(content)).not.toBeNull();

        await TestBed.inject(Router).navigateByUrl('/?period=all');
        fixture.detectChanges();
        expect(getOverview).toHaveBeenLastCalledWith({ allTime: true });
        expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_OVERVIEW.LOADING');
        expect(host.querySelector(content)).toBeNull();

        pending.error(new Error('report unavailable'));
        await fixture.fixture.whenStable();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.ERROR');
        expect(host.querySelector(content)).toBeNull();

        refreshDashboard(fixture);
        await fixture.fixture.whenStable();
        expect(getOverview).toHaveBeenLastCalledWith({ allTime: true });
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(host.querySelector(content)).not.toBeNull();
    });

    it('clears the overview and ignores a pending request when a reversed custom range replaces it', async () => {
        const pending = new Subject<AdminDashboardOverview>();
        const getOverview = vi.fn().mockReturnValue(of(overview));
        const fixture = await renderDashboardAsync(getOverview);
        const router = TestBed.inject(Router);
        const host = fixture.routeNativeElement as HTMLElement;
        getOverview.mockReturnValueOnce(pending);
        await router.navigateByUrl('/?period=custom&from=2024-02-29&to=2024-02-29');
        fixture.detectChanges();
        expect(getOverview).toHaveBeenLastCalledWith({ from: '2024-02-29', to: '2024-02-29' });
        expect(host.querySelector('[role="status"]')).not.toBeNull();
        getOverview.mockClear();

        await router.navigateByUrl('/?period=custom&from=2024-03-01&to=2024-02-29');
        fixture.detectChanges();
        expect(pending.observed).toBe(false);
        pending.next(overview);
        expect(getOverview).not.toHaveBeenCalled();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.INVALID');
        expect(host.querySelector('[role="status"]')).toBeNull();
        expect(host.querySelector('fd-admin-dashboard-content')).toBeNull();
    });
});
