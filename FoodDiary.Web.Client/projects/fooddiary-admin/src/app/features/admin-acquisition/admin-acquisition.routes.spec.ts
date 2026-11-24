import type { ProviderToken } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiInputComponent, FdUiLineChartComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminAcquisitionRoutes } from './admin-acquisition.routes';
import { AdminAcquisitionComparisonComponent } from './components/admin-acquisition-comparison';
import { AdminAcquisitionFacade } from './lib/admin-acquisition.facade';
import type { MarketingAttributionRecentEvent, MarketingAttributionSummary } from './models/admin-acquisition.data';
import type { MarketingAttributionRange } from './models/admin-acquisition-range';

const directEvent: MarketingAttributionRecentEvent = {
    occurredAtUtc: '2024-02-29T10:00:00Z',
    eventType: 'page_landing',
    anonymousId: 'owned-visitor',
    sessionId: 'owned-session',
    landingPath: '/',
    referrerHost: null,
    utmSource: null,
    utmMedium: null,
    utmCampaign: null,
    utmContent: null,
    utmTerm: null,
    buildVersion: null,
};
const summary: MarketingAttributionSummary = {
    windowHours: 72,
    generatedAtUtc: '2024-03-02T00:00:00Z',
    events: 4,
    visits: 2,
    signups: 1,
    premiumStarts: 1,
    anonymousVisitors: 2,
    sessions: 2,
    attributedEvents: 0,
    organicEvents: 4,
    attributedVisits: 0,
    organicVisits: 2,
    signupRatePercent: 50,
    premiumRatePercent: 100,
    lastEventAtUtc: directEvent.occurredAtUtc,
    topCampaigns: [],
    topSources: [],
    recentEvents: [directEvent],
};

describe('Protected acquisition reporting route', () => {
    it('renders a bounded leap-day trend with zero-filled days and passes through a new journal event type', async () => {
        const event = { ...directEvent, eventType: 'newsletter_signup', landingPath: '/newsletter' };
        const getRange = vi.fn((params: Record<string, string | number>) =>
            of({
                ...createRange({ ...summary, recentEvents: [event] }, params),
                byDay: [{ date: '2024-02-29', visits: 2, signups: 1, premiumStarts: 1 }],
            }),
        );
        const harness = await openAcquisitionRouteAsync(getRange, '/acquisition?period=custom&from=2024-02-28&to=2024-03-01');

        const charts = harness.routeDebugElement?.queryAll(By.directive(FdUiLineChartComponent)) ?? [];
        expect(charts.map(chart => chart.injector.get(FdUiLineChartComponent).points())).toEqual([
            [
                { label: '2024-02-28', value: 0 },
                { label: '2024-02-29', value: 2 },
                { label: '2024-03-01', value: 0 },
            ],
            [
                { label: '2024-02-28', value: 0 },
                { label: '2024-02-29', value: 1 },
                { label: '2024-03-01', value: 0 },
            ],
            [
                { label: '2024-02-28', value: 0 },
                { label: '2024-02-29', value: 1 },
                { label: '2024-03-01', value: 0 },
            ],
        ]);
        const journal = harness.routeNativeElement?.querySelector('[aria-labelledby="acquisition-events-title"]');
        expect(journal?.textContent).toContain('newsletter_signup');
        expect(journal?.textContent).toContain('/newsletter');
    });

    it('shows durations returned by the API for a single-day or partial-day observation window', async () => {
        const getRange = vi.fn((params: Record<string, string | number>) => of(createRange({ ...summary, windowHours: 24 }, params)));
        const harness = await openAcquisitionRouteAsync(getRange, '/acquisition?period=custom&from=2024-02-29&to=2024-02-29');
        expect(harness.routeNativeElement?.querySelector('.admin-acquisition__meta')?.textContent).toContain('24 hours');

        getRange.mockImplementationOnce(params => of(createRange({ ...summary, windowHours: 12 }, params)));
        const refresh = harness.routeNativeElement?.querySelector<HTMLButtonElement>(
            '.admin-acquisition__toolbar-actions > fd-ui-button button',
        );
        expect(refresh).toBeDefined();
        refresh?.click();
        await harness.fixture.whenStable();

        expect(harness.routeNativeElement?.querySelector('.admin-acquisition__meta')?.textContent).toContain('12 hours');
    });
});

describe('Acquisition bound filters', () => {
    it('keeps active event filters when child selectors clear their values and submits a cleared search', async () => {
        const getRange = vi.fn((params: Record<string, string | number>) => of(createRange(summary, params)));
        const harness = await openAcquisitionRouteAsync(
            getRange,
            '/acquisition?period=7d&eventType=page_landing&channel=direct&search=prior',
        );
        getRange.mockClear();
        boundControl(harness, 'button#admin-acquisition-event-type', FdUiSelectComponent).value.set(null);
        boundControl(harness, 'button#admin-acquisition-channel', FdUiSelectComponent).value.set(null);
        boundControl(harness, 'input#acquisition-event-search', FdUiInputComponent).value.set(null);
        harness.detectChanges();
        expect(getRange).not.toHaveBeenCalled();

        const apply = harness.routeNativeElement?.querySelector<HTMLButtonElement>('.admin-filter-bar fd-ui-button button');
        expect(apply).toBeDefined();
        apply?.click();
        await harness.fixture.whenStable();

        expect(getRange).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ eventType: 'page_landing', channel: 'direct', search: '' }),
        );
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toMatchObject({ period: '7d', eventType: 'page_landing', channel: 'direct' });
    });

    it('drops the UTM source from a generated link when the bound input clears it', async () => {
        const getRange = vi.fn((params: Record<string, string | number>) => of(createRange(summary, params)));
        const harness = await openAcquisitionRouteAsync(getRange, '/acquisition');
        const url = '.admin-acquisition__url';
        expect(harness.routeNativeElement?.querySelector(url)?.textContent).toContain('utm_source=telegram');
        boundControl(harness, 'input#admin-acquisition-source', FdUiInputComponent).value.set(null);
        harness.detectChanges();

        expect(harness.routeNativeElement?.querySelector(url)?.textContent).not.toContain('utm_source');
        expect(getRange).toHaveBeenCalledOnce();
    });
});

it('removes the comparison charts when a refreshed report is unavailable', () => {
    TestBed.configureTestingModule({
        imports: [AdminAcquisitionComparisonComponent],
        providers: [provideTranslateTesting()],
    });
    const fixture = TestBed.createComponent(AdminAcquisitionComparisonComponent);
    fixture.componentRef.setInput('report', createRange(summary, { fromUtc: '2024-02-28T00:00:00Z', toUtc: '2024-03-02T00:00:00Z' }));
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('fd-ui-line-chart')).not.toBeNull();

    fixture.componentRef.setInput('report', null);
    fixture.detectChanges();

    expect(host.querySelector('fd-ui-line-chart')).toBeNull();
    expect(host.textContent.trim()).toBe('');
});

function boundControl<T>(harness: RouterTestingHarness, selector: string, token: ProviderToken<T>): T {
    return harness.fixture.debugElement.query(By.css(selector)).injector.get(token);
}

async function openAcquisitionRouteAsync(getRange: ReturnType<typeof vi.fn>, url: string): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: 'acquisition', children: adminAcquisitionRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            { provide: AdminAcquisitionFacade, useValue: { getRange } },
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
    const harness = await RouterTestingHarness.create(url);
    await harness.fixture.whenStable();
    return harness;
}

function createRange(current: MarketingAttributionSummary, params: Record<string, string | number>): MarketingAttributionRange {
    return {
        fromUtc: String(params['fromUtc']),
        toUtc: String(params['toUtc']),
        previousFromUtc: '2024-02-25T00:00:00Z',
        current,
        previous: summary,
        byDay: [],
        eventTotal: current.recentEvents.length,
    };
}
