import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { StatisticsSummaryHttpResponse } from '../../shared/api/sdk/generated/model/statistics-summary-http-response';
import { UserService } from '../../shared/api/user.service';
import { MeasurementSystemService } from '../../shared/measurements/measurement-system.service';
import statisticsRoutes from './statistics.routes';

const FROM_DAY = '2026-10-25';
const TO_DAY = '2026-11-09';
const EARLY_BUCKET_END_DAY = '2026-10-26';
const LATE_BUCKET_START_DAY = '2026-11-08';
const REPORT_ROUTE = `/statistics?range=custom&dateFrom=${FROM_DAY}&dateTo=${TO_DAY}`;
const REPORT_API = `${environment.apiUrls.statistics}/summary`;
const WEIGHT_API = environment.apiUrls.weights;
const WAIST_API = environment.apiUrls.waists;
const FIRST_WEIGHT = 74.2;
const EARLY_WEIGHT = 75.4;
const LATE_WEIGHT = 73.8;
const LAST_WEIGHT = 72.4;
const WEIGHT_BUCKET_BEFORE = 74.8;
const WEIGHT_BUCKET_AFTER = 73.1;
const SUMMARY = {
    nutrition: [
        {
            dateFrom: new Date(`${FROM_DAY}T00:00:00.000`).toISOString(),
            dateTo: new Date(`${EARLY_BUCKET_END_DAY}T23:59:59.999`).toISOString(),
            totalCalories: 2200,
            averageProteins: 100,
            averageFats: 70,
            averageCarbs: 250,
            averageFiber: 25,
            totalProteins: 100,
            totalFats: 70,
            totalCarbs: 250,
            totalFiber: 25,
            trackedDayCount: 1,
            mealCount: 1,
        },
    ],
    weight: [
        { startDate: FROM_DAY, endDate: EARLY_BUCKET_END_DAY, averageWeightKg: WEIGHT_BUCKET_BEFORE },
        { startDate: LATE_BUCKET_START_DAY, endDate: TO_DAY, averageWeightKg: WEIGHT_BUCKET_AFTER },
    ],
    waist: [],
} satisfies StatisticsSummaryHttpResponse;
const OBSERVATIONS = [
    { date: FROM_DAY, weightKg: FIRST_WEIGHT },
    { date: EARLY_BUCKET_END_DAY, weightKg: EARLY_WEIGHT },
    { date: LATE_BUCKET_START_DAY, weightKg: LATE_WEIGHT },
    { date: TO_DAY, weightKg: LAST_WEIGHT },
];

it('retries the same local report after a failed measurement request and renders the recovered dates and observations', async () => {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'statistics', children: statisticsRoutes }]),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: UserService, useValue: { user: signal(null) } },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(MeasurementSystemService).setSystem('metric');
    const harness = await RouterTestingHarness.create(REPORT_ROUTE);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the Statistics route');
    }
    const first = expectReportRequests(http);
    first.report.flush(SUMMARY);
    first.weight.flush(OBSERVATIONS);
    harness.detectChanges();
    expect(host.querySelector('.statistics__skeleton-grid')).not.toBeNull();
    expect(host.querySelector('fd-statistics-overview-card')).toBeNull();

    first.waist.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
    harness.detectChanges();
    expect(host.querySelector('fd-error-state [role="alert"]')).not.toBeNull();
    requireElement(host, 'fd-error-state button').click();

    const recovered = expectReportRequests(http);
    recovered.report.flush(SUMMARY);
    recovered.weight.flush(OBSERVATIONS);
    recovered.waist.flush([]);
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe(REPORT_ROUTE);
    expect(host.querySelector('fd-error-state')).toBeNull();
    expect(requireElement(host, 'fd-statistics-overview-card').textContent).toContain('2,200');
    const body = requireElement(host, 'fd-statistics-body-trend-card');
    const current = requireElement(body, '.fd-ui-stat-value');
    expect(current.textContent).toContain('72.4');
    expect(requireElement(body, '.statistics-body-trend-card__change').textContent).toMatch(/[−-]1\.8/u);
    const axis = requireElement(body, 'fd-ui-line-chart-x-axis');
    expect(axis.textContent).toMatch(/Oct\s*25/u);
    expect(axis.textContent).toMatch(/Nov\s*8/u);

    await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
    harness.detectChanges();
    expect(current.textContent).toContain('72,4');
    expect(axis.textContent).toMatch(/25\s*окт/u);
    expect(axis.textContent).toMatch(/8\s*ноя/u);
    http.verify();
});

function expectReportRequests(http: HttpTestingController): { report: TestRequest; weight: TestRequest; waist: TestRequest } {
    const report = http.expectOne(request => request.method === 'GET' && request.url === REPORT_API);
    const weight = http.expectOne(request => request.method === 'GET' && request.url === WEIGHT_API);
    const waist = http.expectOne(request => request.method === 'GET' && request.url === WAIST_API);
    expect(report.request.params.get('dateFrom')).toBe(new Date(`${FROM_DAY}T00:00:00.000`).toISOString());
    expect(report.request.params.get('dateTo')).toBe(new Date(`${TO_DAY}T23:59:59.999`).toISOString());
    expect(report.request.params.get('bodyDateFrom')).toBe(FROM_DAY);
    expect(report.request.params.get('bodyDateTo')).toBe(TO_DAY);
    expect(report.request.params.get('quantizationDays')).toBe('2');
    for (const entryRead of [weight, waist]) {
        expect(entryRead.request.params.get('dateFrom')).toBe(FROM_DAY);
        expect(entryRead.request.params.get('dateTo')).toBe(TO_DAY);
        expect(entryRead.request.params.get('sort')).toBe('asc');
    }
    return { report, weight, waist };
}

function requireElement(root: ParentNode, selector: string): HTMLElement {
    const element = root.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected a rendered report element matching ${selector}`);
    }
    return element;
}
