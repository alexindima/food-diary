import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { expect, it, vi } from 'vitest';

import { AdminRetentionService } from './admin-retention.service';

it('omits absent dates rather than sending undefined for all-time cohorts', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const receive = vi.fn();
    TestBed.inject(AdminRetentionService)
        .getReport({ from: '2026-09-01', to: '2026-09-18', cohortFrom: '1970-01-01', cohortTo: undefined })
        .subscribe(receive);
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(req => req.url.endsWith('/admin/analytics/retention'));
    expect(request.request.params.get('cohortFrom')).toBe('1970-01-01');
    expect(request.request.params.has('cohortTo')).toBe(false);
    expect(request.request.params.get('from')).toBe('2026-09-01');
    const date = '2026-09-18T00:00:00Z';
    const measured = { date: '2026-09-17T00:00:00Z', registered: 1, day1: 0 };
    const newest = { date, registered: 1 };
    const activityByDay = [{ date, activeUsers: 0, mealEntries: 0 }];
    request.flush({
        fromUtc: '2026-09-01T00:00:00Z',
        toUtc: '2026-09-19T00:00:00Z',
        asOfUtc: '2026-09-19T00:00:00Z',
        cohortFromUtc: '1970-01-01T00:00:00Z',
        cohortToUtc: '2026-09-19T00:00:00Z',
        mealEntriesInPeriod: 0,
        activeUsersInPeriod: 0,
        cohorts: [measured, newest],
        activityByDay,
    });
    expect(receive).toHaveBeenCalledExactlyOnceWith(
        expect.objectContaining({
            activityByDay,
            cohorts: [
                { ...measured, activatedWithinSevenDays: null, day7: null, day30: null },
                { ...newest, activatedWithinSevenDays: null, day1: null, day7: null, day30: null },
            ],
        }),
    );
    http.verify();
});
