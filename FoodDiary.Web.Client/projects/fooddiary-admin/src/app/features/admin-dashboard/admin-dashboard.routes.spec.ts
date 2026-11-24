import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDateInputComponent } from 'fd-ui-kit';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminDashboardRoutes } from './admin-dashboard.routes';
import { AdminDashboardService } from './api/admin-dashboard.service';
import type { AdminDashboardOverview } from './models/admin-dashboard-overview.data';

const initialStart = '2024-02-01';
const initialEnd = '2024-02-29';
const editedStart = '2024-02-05';
const epoch = '1970-01-01T00:00:00Z';
const overview: AdminDashboardOverview = {
    fromUtc: `${initialStart}T00:00:00Z`,
    toUtc: '2024-03-01T00:00:00Z',
    interval: 'day',
    period: {
        fromUtc: `${initialStart}T00:00:00Z`,
        toUtc: '2024-03-01T00:00:00Z',
        metrics: { registrations: 1, payingUsers: 0, aiTokens: 0, trend: [] },
        currencies: [],
    },
    previous: null,
    totalUsersNow: 1,
    premiumUsersNow: 0,
    pendingReportsNow: 0,
};
const getOverview = vi.fn<AdminDashboardService['getOverview']>();

beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2024-03-02T14:00:00Z'));
    getOverview.mockReset().mockReturnValue(of(overview));
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter(
                [{ path: 'dashboard', children: adminDashboardRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            { provide: AdminDashboardService, useValue: { getOverview } },
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
        ],
    });
});

afterEach(() => {
    vi.useRealTimers();
});

describe('Dashboard applied period', () => {
    it('refreshes the applied range without losing the unsubmitted calendar edit', async () => {
        const path = `/dashboard?period=custom&from=${initialStart}&to=${initialEnd}`;
        const harness = await RouterTestingHarness.create(path);
        expect(getOverview).toHaveBeenLastCalledWith({ from: initialStart, to: initialEnd });
        await selectFromDayAsync(harness, editedStart);
        expect(getOverview).toHaveBeenCalledOnce();

        toolbarButton(harness, 'REFRESH').click();
        harness.detectChanges();
        expect(getOverview).toHaveBeenLastCalledWith({ from: initialStart, to: initialEnd });
        expect(TestBed.inject(Router).url).toBe(path);

        toolbarButton(harness, 'APPLY').click();
        await harness.fixture.whenStable();
        expect(getOverview).toHaveBeenLastCalledWith({ from: editedStart, to: initialEnd });
        expect(TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams).toMatchObject({
            period: 'custom',
            from: editedStart,
            to: initialEnd,
        });
    });

    it('keeps an all-time range valid in each rendered detail link', async () => {
        getOverview.mockReturnValueOnce(
            of({ ...overview, fromUtc: epoch, interval: 'month', period: { ...overview.period, fromUtc: epoch } }),
        );
        const harness = await RouterTestingHarness.create('/dashboard?period=all');
        expect(getOverview).toHaveBeenCalledExactlyOnceWith({ allTime: true });

        for (const path of ['/billing', '/users', '/ai-usage']) {
            const link = harness.routeNativeElement?.querySelector<HTMLAnchorElement>(`a[href^="${path}?"]`);
            expect(link?.pathname).toBe(path);
            expect(Object.fromEntries(new URLSearchParams(link?.search))).toEqual({ period: 'all' });
        }
    });
});

describe('Dashboard preset selection', () => {
    it('applies a relative period across leap day, clears stale bounds and validates a new custom draft', async () => {
        stubScrollIntoView();
        const harness = await RouterTestingHarness.create(`/dashboard?period=custom&from=${initialStart}&to=${initialEnd}`);
        const router = TestBed.inject(Router);
        await choosePresetAsync(harness, 'DAYS7');
        expect(getOverview).toHaveBeenLastCalledWith({ from: '2024-02-25', to: '2024-03-02' });
        expect(router.url).toBe('/dashboard?period=7d');
        expect(harness.routeNativeElement?.querySelector('fd-ui-date-input')).toBeNull();

        getOverview.mockClear();
        await choosePresetAsync(harness, 'CUSTOM');
        expect(harness.routeNativeElement?.querySelectorAll('fd-ui-date-input')).toHaveLength(2);
        toolbarButton(harness, 'APPLY').click();
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_OVERVIEW.INVALID');
        expect(getOverview).not.toHaveBeenCalled();
        expect(router.url).toBe('/dashboard?period=7d');
    });

    it('keeps the local calendar date when the start control publishes a Date model value', async () => {
        const harness = await RouterTestingHarness.create(`/dashboard?period=custom&from=${initialStart}&to=${initialEnd}`);
        const control = harness.fixture.debugElement.query(By.directive(FdUiDateInputComponent)).injector.get(FdUiDateInputComponent);
        control.value.set(new Date(`${editedStart}T00:15:00`));
        harness.detectChanges();

        toolbarButton(harness, 'APPLY').click();
        await harness.fixture.whenStable();

        expect(getOverview).toHaveBeenLastCalledWith({ from: editedStart, to: initialEnd });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toMatchObject({ from: editedStart, to: initialEnd });
    });
});

async function choosePresetAsync(harness: RouterTestingHarness, key: string): Promise<void> {
    const trigger = harness.routeNativeElement?.querySelector<HTMLButtonElement>('.admin-dashboard__toolbar fd-ui-select button');
    if (trigger === null || trigger === undefined) {
        throw new Error('Expected the dashboard period selector');
    }
    trigger.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    const options = TestBed.inject(DOCUMENT).querySelectorAll<HTMLButtonElement>('[role="option"]');
    const choice = Array.from(options).find(option => option.textContent.trim() === `ADMIN_OVERVIEW.${key}`);
    if (choice === undefined) {
        throw new Error(`Expected dashboard preset ${key}`);
    }
    choice.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();
}

async function selectFromDayAsync(harness: RouterTestingHarness, date: string): Promise<void> {
    const control = harness.routeNativeElement?.querySelector<HTMLInputElement>('fd-ui-date-input input');
    if (control === null || control === undefined) {
        throw new Error('Expected the dashboard start date field');
    }
    control.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    const day = TestBed.inject(DOCUMENT).querySelector<HTMLButtonElement>(`[role="gridcell"][data-date="${date}"]`);
    if (day === null) {
        throw new Error(`Expected calendar day ${date}`);
    }
    day.click();
    harness.detectChanges();
}

function toolbarButton(harness: RouterTestingHarness, action: string): HTMLButtonElement {
    const buttons = harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('.admin-dashboard__toolbar button') ?? [];
    const button = Array.from(buttons).find(item => item.textContent.trim() === `ADMIN_OVERVIEW.${action}`);
    if (button === undefined) {
        throw new Error(`Expected dashboard action ${action}`);
    }
    return button;
}
