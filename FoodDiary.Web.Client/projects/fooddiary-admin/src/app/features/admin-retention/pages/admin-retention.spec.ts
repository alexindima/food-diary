import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminRetentionService } from '../api/admin-retention.service';
import type { AdminRetentionReport } from '../models/admin-retention';
import { AdminRetentionComponent } from './admin-retention';

const report: AdminRetentionReport = {
    fromUtc: '2026-01-01T00:00:00Z',
    cohortFromUtc: '2025-11-01T00:00:00Z',
    cohortToUtc: '2026-01-03T00:00:00Z',
    toUtc: '2026-01-03T00:00:00Z',
    mealEntriesInPeriod: 3,
    asOfUtc: '2026-02-01T00:00:00Z',
    activeUsersInPeriod: 0,
    activityByDay: [
        { date: '2026-01-01T00:00:00Z', activeUsers: 1, mealEntries: 3 },
        { date: '2026-01-02T00:00:00Z', activeUsers: 0, mealEntries: 0 },
    ],
    cohorts: [{ date: '2026-01-01T00:00:00Z', registered: 2, activatedWithinSevenDays: 0, day1: 0, day7: 0, day30: null }],
};

const RETENTION_PATH = '/analytics/retention';
const getReport = vi.fn();

beforeEach(async () => {
    getReport.mockReset().mockReturnValue(of(report));
    await TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: 'analytics/retention', component: AdminRetentionComponent }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            ...provideTranslateTesting(),
            { provide: AdminRetentionService, useValue: { getReport } },
        ],
    }).compileComponents();
});

describe('Admin retention display', () => {
    it('renders a mature zero separately from a cohort whose observation window is incomplete', async () => {
        const { host } = await openRetentionAsync();
        const text = host.textContent;
        expect(text).toContain('Jan 1, 2026');
        expect(text).toContain('0.0% (0/2)');
        expect(text).toContain('ADMIN_RETENTION.IMMATURE');
    });

    it('sends independent activity and cohort periods and renders zero days', async () => {
        const { host } = await openRetentionAsync(
            '?period=custom&from=2025-11-01&to=2025-12-01&activity_period=custom&activity_from=2026-01-01&activity_from=ignored&activity_to=2026-01-02',
        );
        expect(getReport).toHaveBeenLastCalledWith({
            from: '2026-01-01',
            to: '2026-01-02',
            cohortFrom: '2025-11-01',
            cohortTo: '2025-12-01',
        });
        expect(host.textContent).toContain('Jan 2, 2026');
        expect(host.querySelectorAll('tbody')[0].rows[1].textContent).toMatch(/Jan 2, 2026\s*0\s*0/);
        expect(host.querySelector('details')).toBeNull();
    });

    it('shows failure and can retry without presenting a false empty report', async () => {
        getReport.mockReturnValueOnce(throwError(() => new Error('offline')));
        const { harness, host } = await openRetentionAsync();
        expect(host.querySelector('table')).toBeNull();
        const retry = host.querySelector<HTMLButtonElement>('fd-admin-load-error button');
        expect(retry).not.toBeNull();
        retry?.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(host.querySelector('fd-admin-load-error')).toBeNull();
        expect(host.querySelectorAll('table')).toHaveLength(2);
    });
});

describe('Admin retention period transitions', () => {
    it.each([
        { period: 'custom', from: '2024-02-30', to: '2024-03-01' },
        { activity_period: 'custom', activity_from: '2024-03-01', activity_to: '2024-02-29' },
    ])('hides a previous report and cancels a pending request when a filter becomes invalid %o', async invalid => {
        const pending = new Subject<AdminRetentionReport>();
        const { harness, host } = await openRetentionAsync();
        expect(host.querySelectorAll('table')).toHaveLength(2);

        getReport.mockReturnValueOnce(pending);
        await harness.navigateByUrl(`${RETENTION_PATH}?period=all`, AdminRetentionComponent);
        expect(pending.observed).toBe(true);
        expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
        expect(host.querySelector('table')).toBeNull();

        const invalidQuery = new URLSearchParams(
            Object.entries(invalid).filter((entry): entry is [string, string] => entry[1] !== undefined),
        );
        await harness.navigateByUrl(`${RETENTION_PATH}?${invalidQuery}`, AdminRetentionComponent);
        expect(pending.observed).toBe(false);
        pending.next(report);
        expect(getReport).toHaveBeenCalledTimes(2);
        expect(host.querySelector('table')).toBeNull();
        expect(host.querySelector('[role="status"]')).toBeNull();
    });

    it('requests all-time activity and cohorts and displays a dash for a zero-denominator cohort', async () => {
        const cohort = { ...report.cohorts[0], registered: 0 };
        getReport.mockReturnValueOnce(of({ ...report, activityByDay: [], cohorts: [cohort] }));
        const { host } = await openRetentionAsync('?period=all&activity_period=all');

        expect(getReport).toHaveBeenCalledExactlyOnceWith({ from: '1970-01-01', cohortFrom: '1970-01-01', cohortTo: undefined });
        const row = host.querySelector('[aria-labelledby="retention-cohorts-title"] tbody tr');
        expect(Array.from(row?.querySelectorAll('td') ?? [], cell => cell.textContent.trim())).toEqual([
            '0',
            '—',
            '—',
            '—',
            'ADMIN_RETENTION.IMMATURE',
        ]);
        expect(host.textContent).toContain('ADMIN_RETENTION.NO_ACTIVITY');
    });
});

async function openRetentionAsync(query = ''): Promise<{ harness: RouterTestingHarness; host: HTMLElement }> {
    const harness = await RouterTestingHarness.create(`${RETENTION_PATH}${query}`);
    await harness.fixture.whenStable();
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the retention report to render.');
    }
    return { harness, host };
}
