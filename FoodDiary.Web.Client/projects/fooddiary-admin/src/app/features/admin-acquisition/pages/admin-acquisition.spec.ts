import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';
import { AdminAcquisitionComponent } from './admin-acquisition';

describe('AdminAcquisitionComponent', () => {
    const EXPECTED_TABLE_COUNT = 3;
    const SEVEN_DAYS_MS = 604_800_000;

    let fixture: RouterTestingHarness;
    const summary = createSummary();
    const facade = {
        getRange: vi.fn((params: Record<string, string | number>) => of(createRange(summary, params))),
    };

    beforeEach(async () => {
        facade.getRange.mockClear();
        fixture = await createReportingFixtureAsync(facade.getRange);
        await fixture.fixture.whenStable();
        fixture.detectChanges();
    });

    it('renders analysis-first KPIs and comparable tables', () => {
        const element = fixture.routeNativeElement as HTMLElement;

        expect(element.textContent).toContain('Tracked visits');
        expect(element.textContent).toContain('50.0%');
        expect(element.textContent).toContain('Campaign performance');
        expect(element.textContent).toContain('Channel performance');
        expect(element.querySelectorAll('table')).toHaveLength(EXPECTED_TABLE_COUNT);
        expect(facade.getRange).toHaveBeenCalledWith(expect.objectContaining({ page: 1, limit: 50 }));
        expect(element.querySelector('fd-admin-acquisition-comparison')?.textContent).toContain('Δ');
    });

    it('shows all-time totals without inventing a comparison with dates before the epoch', async () => {
        await TestBed.inject(Router).navigate([], { queryParams: { period: 'all' } });
        await fixture.fixture.whenStable();
        fixture.detectChanges();
        const comparison = fixture.routeNativeElement?.querySelector('fd-admin-acquisition-comparison');
        expect(comparison?.textContent).not.toContain('Δ');
        expect(comparison?.querySelectorAll(':scope > p')).toHaveLength(1);
        const values = Array.from(comparison?.querySelectorAll('fd-ui-card p') ?? []).map(element => element.textContent.trim());
        expect(values).toEqual(['2', '1', '1']);
    });

    it('reloads data when the reporting period changes', async () => {
        await TestBed.inject(Router).navigate([], { queryParams: { period: '7d' } });
        await fixture.fixture.whenStable();
        const params = facade.getRange.mock.lastCall?.[0];
        expect(Date.parse(String(params?.['toUtc'])) - Date.parse(String(params?.['fromUtc']))).toBe(SEVEN_DAYS_MS);
    });

    it('filters the event log by attribution state', async () => {
        stubScrollIntoView();
        const element = fixture.routeNativeElement as HTMLElement;
        const select = element.querySelector<HTMLButtonElement>('button#admin-acquisition-channel');
        expect(select).not.toBeNull();
        select?.click();
        fixture.detectChanges();
        const option = Array.from(TestBed.inject(DOCUMENT).querySelectorAll<HTMLElement>('[role=option]')).find(item =>
            item.textContent.includes('Tracked only'),
        );
        expect(option).toBeDefined();
        option?.click();
        await fixture.fixture.whenStable();
        fixture.detectChanges();

        expect(facade.getRange).toHaveBeenLastCalledWith(expect.objectContaining({ channel: 'tracked', page: 1 }));
        expect(element.textContent).toContain('telegram / social / launch');
        expect(element.textContent).not.toContain('/direct-only');
    });

    it('renders distinct journal events sharing a timestamp and session without duplicate tracking keys', async () => {
        const warning = vi.spyOn(console, 'warn').mockImplementation(vi.fn());
        facade.getRange.mockImplementationOnce(params => of(createRange(createTiedSummary(summary), params)));
        try {
            await TestBed.inject(Router).navigate([], { queryParams: { search: 'tied-events' } });
            await fixture.fixture.whenStable();
            fixture.detectChanges();
            assertTiedJournalRows(fixture.routeNativeElement as HTMLElement);
            expect(warning.mock.calls.some(call => call.some(value => /NG095[56]/u.test(String(value))))).toBe(false);
        } finally {
            warning.mockRestore();
        }
    });
});

describe('Acquisition reporting boundaries', () => {
    it('hides stale journal results and aborts the pending leap-day report when the URL range becomes invalid', async () => {
        const pending = new Subject<MarketingAttributionRange>();
        const getRange = vi.fn().mockReturnValue(pending);
        const fixture = await createReportingFixtureAsync(getRange);
        const router = TestBed.inject(Router);
        const host = fixture.routeNativeElement as HTMLElement;
        await router.navigateByUrl('/?period=custom&from=2024-02-29&to=2024-02-29&eventType=unknown&channel=unknown');
        fixture.detectChanges();
        expect(getRange).toHaveBeenLastCalledWith(
            expect.objectContaining({ fromUtc: '2024-02-29T00:00:00Z', toUtc: '2024-03-01T00:00:00.000Z', eventType: '', channel: '' }),
        );
        expect(host.textContent).toContain('Loading acquisition data...');
        expect(pending.observed).toBe(true);
        getRange.mockClear();

        await router.navigateByUrl('/?period=custom&from=2024-02-30&to=2024-03-01');
        fixture.detectChanges();
        expect(pending.observed).toBe(false);
        pending.next(createRange(createSummary(), { fromUtc: '2024-02-29T00:00:00Z', toUtc: '2024-03-01T00:00:00.000Z' }));
        expect(getRange).not.toHaveBeenCalled();
        expect(host.textContent).not.toContain('Loading acquisition data...');
        expect(host.querySelector('[aria-labelledby="acquisition-events-title"]')).toBeNull();
    });

    it('retains filtered dates at the last page and shows a referral without UTM tags', async () => {
        const summary = createSummary();
        const referral = { ...summary.recentEvents[1], eventType: 'premium_started', referrerHost: 'partner.example.test' };
        const getRange = vi.fn((params: Record<string, string | number>) =>
            of({
                ...createRange({ ...summary, recentEvents: params['page'] === 2 ? [referral] : summary.recentEvents }, params),
                eventTotal: 51,
            }),
        );
        const fixture = await createReportingFixtureAsync(getRange);
        const router = TestBed.inject(Router);
        await router.navigateByUrl('/?period=7d&search=partner');
        await fixture.fixture.whenStable();
        fixture.detectChanges();
        const host = fixture.routeNativeElement as HTMLElement;
        const next = Array.from(host.querySelectorAll('button')).find(button => button.textContent.includes('ADMIN_COMMON.NEXT'));
        expect(next).toBeDefined();
        next?.click();
        await fixture.fixture.whenStable();
        fixture.detectChanges();

        expect(router.parseUrl(router.url).queryParams).toMatchObject({ period: '7d', search: 'partner', page: '2' });
        expect(getRange).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'partner', page: 2, limit: 50 }));
        const journal = host.querySelector('[aria-labelledby="acquisition-events-title"]');
        expect(journal?.querySelectorAll('tbody tr')).toHaveLength(1);
        expect(journal?.textContent).toContain('partner.example.test / referral / none');
        expect(journal?.textContent).toContain('Premium');
        const finalNext = Array.from(host.querySelectorAll('button')).find(button => button.textContent.includes('ADMIN_COMMON.NEXT'));
        expect(finalNext?.disabled).toBe(true);
    });
});

describe('Acquisition event selector', () => {
    it('restores an out-of-range journal page and filters through the displayed event menu', async () => {
        stubScrollIntoView();
        const summary = createSummary();
        const getRange = vi.fn((params: Record<string, string | number>) => {
            const recentEvents = params['eventType'] === 'premium_started' ? [] : summary.recentEvents;
            return of(createRange({ ...summary, recentEvents }, params));
        });
        const fixture = await createReportingFixtureAsync(getRange);
        const router = TestBed.inject(Router);
        const host = fixture.routeNativeElement as HTMLElement;
        await router.navigateByUrl('/?period=7d&page=9&channel=tracked');
        await fixture.fixture.whenStable();
        fixture.detectChanges();
        expect(getRange).toHaveBeenCalledWith(expect.objectContaining({ page: 9, channel: 'tracked' }));
        expect(getRange).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, channel: 'tracked' }));
        expect(host.querySelector('[aria-labelledby="acquisition-events-title"]')?.textContent).toContain('/pricing');

        const select = host.querySelector<HTMLButtonElement>('button#admin-acquisition-event-type');
        expect(select).not.toBeNull();
        select?.click();
        fixture.detectChanges();
        const options = TestBed.inject(DOCUMENT).querySelectorAll<HTMLElement>('[role="option"]');
        const premium = Array.from(options).find(option => option.textContent.trim() === 'Premium starts');
        expect(premium).toBeDefined();
        premium?.click();
        await fixture.fixture.whenStable();
        fixture.detectChanges();

        expect(router.parseUrl(router.url).queryParams).toMatchObject({
            period: '7d',
            channel: 'tracked',
            eventType: 'premium_started',
            page: '1',
        });
        expect(getRange).toHaveBeenLastCalledWith(expect.objectContaining({ eventType: 'premium_started', channel: 'tracked', page: 1 }));
        expect(host.querySelector('[aria-labelledby="acquisition-events-title"]')?.textContent).toContain('No events match these filters.');
    });
});

async function createReportingFixtureAsync(getRange: ReturnType<typeof vi.fn>): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
        imports: [AdminAcquisitionComponent],
        providers: [
            provideRouter(
                [{ path: '', component: AdminAcquisitionComponent }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            ...provideTranslateTesting(),
            { provide: AdminAcquisitionFacade, useValue: { getRange } },
        ],
    });
    return RouterTestingHarness.create('/');
}

function assertTiedJournalRows(element: HTMLElement): void {
    const rows = element.querySelectorAll('[aria-labelledby="acquisition-events-title"] tbody tr');
    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('/pricing');
    expect(rows[1].textContent).toContain('/direct-only');
}

function createTiedSummary(summary: MarketingAttributionSummary): MarketingAttributionSummary {
    return {
        ...summary,
        recentEvents: summary.recentEvents.map(event => ({
            ...event,
            occurredAtUtc: '2026-07-24T12:00:00Z',
            sessionId: 'shared-session',
        })),
    };
}

function createRange(summary: MarketingAttributionSummary, params: Record<string, string | number>): MarketingAttributionRange {
    const events = params['channel'] === 'tracked' ? summary.recentEvents.filter(item => item.utmSource !== null) : summary.recentEvents;
    return {
        fromUtc: String(params['fromUtc']),
        toUtc: String(params['toUtc']),
        previousFromUtc: '2026-06-01T00:00:00Z',
        current: { ...summary, recentEvents: events },
        previous: summary,
        byDay: [],
        eventTotal: events.length,
    };
}

// eslint-disable-next-line max-lines-per-function -- A complete response fixture keeps all dashboard sections covered together.
function createSummary(): MarketingAttributionSummary {
    return {
        windowHours: 720,
        generatedAtUtc: '2026-07-24T12:00:00Z',
        events: 4,
        visits: 2,
        signups: 1,
        premiumStarts: 1,
        anonymousVisitors: 2,
        sessions: 2,
        attributedEvents: 3,
        organicEvents: 1,
        attributedVisits: 1,
        organicVisits: 1,
        signupRatePercent: 50,
        premiumRatePercent: 100,
        lastEventAtUtc: '2026-07-24T12:00:00Z',
        topCampaigns: [
            {
                source: 'telegram',
                medium: 'social',
                campaign: 'launch',
                events: 3,
                visits: 1,
                signups: 1,
                premiumStarts: 1,
                anonymousVisitors: 1,
                sessions: 1,
                signupRatePercent: 100,
                premiumRatePercent: 100,
                lastEventAtUtc: '2026-07-24T12:00:00Z',
            },
        ],
        topSources: [
            {
                source: 'telegram',
                medium: 'social',
                campaign: 'all',
                events: 3,
                visits: 1,
                signups: 1,
                premiumStarts: 1,
                anonymousVisitors: 1,
                sessions: 1,
                signupRatePercent: 100,
                premiumRatePercent: 100,
                lastEventAtUtc: '2026-07-24T12:00:00Z',
            },
            {
                source: 'direct',
                medium: 'none',
                campaign: 'all',
                events: 1,
                visits: 1,
                signups: 0,
                premiumStarts: 0,
                anonymousVisitors: 1,
                sessions: 1,
                signupRatePercent: 0,
                premiumRatePercent: 0,
                lastEventAtUtc: '2026-07-24T11:00:00Z',
            },
        ],
        recentEvents: [
            {
                occurredAtUtc: '2026-07-24T12:00:00Z',
                eventType: 'signup_completed',
                anonymousId: 'tracked-visitor',
                sessionId: 'tracked-session',
                landingPath: '/pricing',
                referrerHost: 't.me',
                utmSource: 'telegram',
                utmMedium: 'social',
                utmCampaign: 'launch',
                utmContent: 'creative-a',
                utmTerm: null,
                buildVersion: 'test',
            },
            {
                occurredAtUtc: '2026-07-24T11:00:00Z',
                eventType: 'page_landing',
                anonymousId: 'direct-visitor',
                sessionId: 'direct-session',
                landingPath: '/direct-only',
                referrerHost: null,
                utmSource: null,
                utmMedium: null,
                utmCampaign: null,
                utmContent: null,
                utmTerm: null,
                buildVersion: 'test',
            },
        ],
    };
}
