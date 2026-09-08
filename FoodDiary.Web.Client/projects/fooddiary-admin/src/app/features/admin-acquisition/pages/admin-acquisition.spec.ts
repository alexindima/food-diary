import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import { AdminAcquisitionComponent } from './admin-acquisition';

describe('AdminAcquisitionComponent', () => {
    const EXPECTED_TABLE_COUNT = 3;
    const SEVEN_DAYS_MS = 604_800_000;

    let fixture: ComponentFixture<AdminAcquisitionComponent>;
    const summary = createSummary();
    const facade = {
        getRange: vi.fn((params: Record<string, string | number>) => {
            const events =
                params['channel'] === 'tracked' ? summary.recentEvents.filter(item => item.utmSource !== null) : summary.recentEvents;
            return of({
                fromUtc: String(params['fromUtc']),
                toUtc: String(params['toUtc']),
                previousFromUtc: '2026-06-01T00:00:00Z',
                current: { ...summary, recentEvents: events },
                previous: summary,
                byDay: [],
                eventTotal: events.length,
            });
        }),
    };

    beforeEach(async () => {
        facade.getRange.mockClear();
        await TestBed.configureTestingModule({
            imports: [AdminAcquisitionComponent],
            providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminAcquisitionFacade, useValue: facade }],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminAcquisitionComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    });

    it('renders analysis-first KPIs and comparable tables', () => {
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('Tracked visits');
        expect(element.textContent).toContain('50.0%');
        expect(element.textContent).toContain('Campaign performance');
        expect(element.textContent).toContain('Channel performance');
        expect(element.querySelectorAll('table')).toHaveLength(EXPECTED_TABLE_COUNT);
        expect(facade.getRange).toHaveBeenCalledWith(expect.objectContaining({ page: 1, limit: 50 }));
    });

    it('reloads data when the reporting period changes', async () => {
        await TestBed.inject(Router).navigate([], { queryParams: { period: '7d' } });
        await fixture.whenStable();
        const params = facade.getRange.mock.lastCall?.[0];
        expect(Date.parse(String(params?.['toUtc'])) - Date.parse(String(params?.['fromUtc']))).toBe(SEVEN_DAYS_MS);
    });

    it('filters the event log by attribution state', async () => {
        const element = fixture.nativeElement as HTMLElement;
        const select = getRequiredSelect(element, '#admin-acquisition-channel');

        select.value = 'tracked';
        select.dispatchEvent(new Event('change'));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(facade.getRange).toHaveBeenLastCalledWith(expect.objectContaining({ channel: 'tracked', page: 1 }));
        expect(element.textContent).toContain('telegram / social / launch');
        expect(element.textContent).not.toContain('/direct-only');
    });
});

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

function getRequiredSelect(element: HTMLElement, selector: string): HTMLSelectElement {
    const select = element.querySelector<HTMLSelectElement>(selector);
    if (select === null) {
        throw new Error(`Expected select ${selector}`);
    }

    return select;
}
