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
        {
            startDate: `${FROM_DAY}T00:00:00Z`,
            endDate: `${EARLY_BUCKET_END_DAY}T00:00:00Z`,
            averageWeightKg: WEIGHT_BUCKET_BEFORE,
        },
        {
            startDate: `${LATE_BUCKET_START_DAY}T00:00:00Z`,
            endDate: `${TO_DAY}T00:00:00Z`,
            averageWeightKg: WEIGHT_BUCKET_AFTER,
        },
    ],
    waist: [],
} satisfies StatisticsSummaryHttpResponse;
const OBSERVATIONS = [
    { date: FROM_DAY, weightKg: FIRST_WEIGHT },
    { date: EARLY_BUCKET_END_DAY, weightKg: EARLY_WEIGHT },
    { date: LATE_BUCKET_START_DAY, weightKg: LATE_WEIGHT },
    { date: TO_DAY, weightKg: LAST_WEIGHT },
];

function configureReportHttp(): HttpTestingController {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'statistics', children: statisticsRoutes }]),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: UserService, useValue: { user: signal(null) } },
        ],
    });
    return TestBed.inject(HttpTestingController);
}

it('retries the same local report after a failed measurement request and renders the recovered dates and observations', async () => {
    const http = configureReportHttp();
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

it('shows a logged zero-calorie interval in the chart and leaves an empty interval blank', async () => {
    const http = configureReportHttp();
    const harness = await RouterTestingHarness.create(REPORT_ROUTE);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the Statistics route');
    }
    const requests = expectReportRequests(http);
    const logged = {
        ...SUMMARY.nutrition[0],
        totalCalories: 0,
        averageProteins: 0,
        averageFats: 0,
        averageCarbs: 0,
        averageFiber: 0,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
    };
    const empty = {
        ...logged,
        dateFrom: new Date(`${LATE_BUCKET_START_DAY}T00:00:00.000`).toISOString(),
        dateTo: new Date(`${TO_DAY}T23:59:59.999`).toISOString(),
        trackedDayCount: 0,
        mealCount: 0,
    };
    requests.report.flush({ nutrition: [logged, empty], waist: [], weight: [] } satisfies StatisticsSummaryHttpResponse);
    requests.weight.flush([]);
    requests.waist.flush([]);
    harness.detectChanges();

    const trend = requireElement(host, '.statistics__nutrition-group:not([hidden]) fd-statistics-nutrition-trend-card');
    const categories = trend.querySelectorAll('.fd-ui-bar-chart__category');
    expect(categories).toHaveLength(2);
    expect(trend.querySelectorAll('.fd-ui-bar-chart__categorical-bar')).toHaveLength(1);
    expect(categories[0].querySelector('[role="img"]')?.getAttribute('aria-label')).toMatch(/Oct\s*25: 0/u);
    expect(categories[1].querySelector('[role="img"]')).toBeNull();
    http.verify();
});

it('keeps the stored measurement date when the viewer time zone skipped that calendar day', async () => {
    const from = '2011-12-29';
    const measured = '2011-12-30';
    const to = '2011-12-31';
    const route = `/statistics?range=custom&dateFrom=${from}&dateTo=${to}`;
    const http = configureReportHttp();
    TestBed.inject(MeasurementSystemService).setSystem('metric');
    const harness = await RouterTestingHarness.create(route);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the Statistics route');
    }
    const requests = expectReportRequests(http, { from, to, quantizationDays: '1' });
    const measurements = [
        { date: from, weightKg: FIRST_WEIGHT },
        { date: measured, weightKg: LAST_WEIGHT },
    ];
    const weight = measurements.map(({ date, weightKg }) => ({
        startDate: `${date}T00:00:00Z`,
        endDate: `${date}T00:00:00Z`,
        averageWeightKg: weightKg,
    }));
    requests.report.flush({ nutrition: [], waist: [], weight } satisfies StatisticsSummaryHttpResponse);
    requests.weight.flush(measurements);
    requests.waist.flush([]);
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe(route);
    const body = requireElement(host, 'fd-statistics-body-trend-card');
    expect(requireElement(body, '.fd-ui-stat-value').textContent).toContain('72.4');
    expect(requireElement(body, '.statistics-body-trend-card__change').textContent).toMatch(/[−-]1\.8/u);
    const plotted = body.querySelectorAll<HTMLElement>('.fd-ui-line-chart__point');
    expect(plotted).toHaveLength(2);
    expect(Number.parseFloat(plotted[0].style.left)).toBeLessThan(Number.parseFloat(plotted[1].style.left));
    expect(Number.parseFloat(plotted[0].style.top)).toBeLessThan(Number.parseFloat(plotted[1].style.top));
    expect(plotted[0].title).toMatch(/Dec\s*29: 74\.2/u);
    expect(plotted[1].title).toMatch(/Dec\s*30: 72\.4/u);
    const axis = requireElement(body, 'fd-ui-line-chart-x-axis');
    expect(axis.textContent).toMatch(/Dec\s*29/u);
    expect(axis.textContent).toMatch(/Dec\s*30/u);
    expect(axis.textContent).not.toMatch(/Dec\s*31/u);
    http.verify();
});

function expectReportRequests(
    http: HttpTestingController,
    { from = FROM_DAY, to = TO_DAY, quantizationDays = '2' } = {},
): { report: TestRequest; weight: TestRequest; waist: TestRequest } {
    const report = http.expectOne(request => request.method === 'GET' && request.url === REPORT_API);
    const weight = http.expectOne(request => request.method === 'GET' && request.url === WEIGHT_API);
    const waist = http.expectOne(request => request.method === 'GET' && request.url === WAIST_API);
    expect(report.request.params.get('dateFrom')).toBe(new Date(`${from}T00:00:00.000`).toISOString());
    expect(report.request.params.get('dateTo')).toBe(new Date(`${to}T23:59:59.999`).toISOString());
    expect(report.request.params.get('bodyDateFrom')).toBe(from);
    expect(report.request.params.get('bodyDateTo')).toBe(to);
    expect(report.request.params.get('quantizationDays')).toBe(quantizationDays);
    for (const entryRead of [weight, waist]) {
        expect(entryRead.request.params.get('dateFrom')).toBe(from);
        expect(entryRead.request.params.get('dateTo')).toBe(to);
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
