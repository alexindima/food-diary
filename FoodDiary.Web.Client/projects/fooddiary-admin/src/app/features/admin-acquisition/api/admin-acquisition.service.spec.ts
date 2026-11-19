import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import { AdminAcquisitionService, DEFAULT_ACQUISITION_WINDOW_HOURS } from './admin-acquisition.service';

const summary: MarketingAttributionSummary = {
    windowHours: DEFAULT_ACQUISITION_WINDOW_HOURS,
    generatedAtUtc: '2026-10-07T00:00:00Z',
    events: 0,
    visits: 0,
    signups: 0,
    premiumStarts: 0,
    anonymousVisitors: 0,
    sessions: 0,
    attributedEvents: 0,
    organicEvents: 0,
    attributedVisits: 0,
    organicVisits: 0,
    signupRatePercent: 0,
    premiumRatePercent: 0,
    lastEventAtUtc: null,
    topCampaigns: [],
    topSources: [],
    recentEvents: [],
};

let service: AdminAcquisitionFacade;
let httpMock: HttpTestingController;

const summaryUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/acquisition/summary`;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [AdminAcquisitionService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AdminAcquisitionFacade);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('AdminAcquisitionService summary', () => {
    const SELECTED_WINDOW_HOURS = 168;

    it('requests the default reporting window', () => {
        service.getSummary().subscribe();

        const request = httpMock.expectOne(
            candidate => candidate.url === summaryUrl && candidate.params.get('hours') === DEFAULT_ACQUISITION_WINDOW_HOURS.toString(),
        );
        expect(request.request.method).toBe('GET');
        request.flush(summary);
    });

    it('requests a selected reporting window', () => {
        service.getSummary(SELECTED_WINDOW_HOURS).subscribe();

        const request = httpMock.expectOne(
            candidate => candidate.url === summaryUrl && candidate.params.get('hours') === SELECTED_WINDOW_HOURS.toString(),
        );
        expect(request.request.method).toBe('GET');
        request.flush(summary);
    });

    it('keeps visit metrics usable while an older API version is still serving requests', () => {
        let result: MarketingAttributionSummary | undefined;
        service.getSummary().subscribe(value => {
            result = value;
        });

        const request = httpMock.expectOne(candidate => candidate.url === summaryUrl);
        const direct = {
            occurredAtUtc: '2026-10-06T11:25:00Z',
            eventType: 'page_landing',
            anonymousId: 'direct-id',
            sessionId: 'direct-session',
            landingPath: '/',
        };
        request.flush({ ...summary, visits: 52, attributedVisits: undefined, organicVisits: undefined, recentEvents: [direct] });

        expect(result).toMatchObject({
            attributedVisits: 0,
            organicVisits: 52,
            recentEvents: [{ ...direct, utmSource: null, referrerHost: null }],
        });
    });
});

describe('AdminAcquisitionService range', () => {
    it('passes date bounds and journal filters to the range endpoint and normalizes optional attribution', () => {
        const filters = {
            fromUtc: '2024-02-29T00:00:00Z',
            toUtc: '2024-03-01T00:00:00Z',
            page: 2,
            limit: 50,
            eventType: 'signup_completed',
            channel: 'tracked',
            search: 'newsletter + February',
        };
        const breakdown = {
            source: 'newsletter',
            medium: 'email',
            campaign: 'february',
            events: 1,
            visits: 1,
            signups: 0,
            premiumStarts: 0,
            anonymousVisitors: 1,
            sessions: 1,
            signupRatePercent: 0,
            premiumRatePercent: 0,
        };
        const event = {
            occurredAtUtc: '2024-02-29T08:30:00Z',
            eventType: 'signup_completed',
            anonymousId: 'owned-anonymous',
            sessionId: 'owned-session',
            landingPath: '/pricing',
            utmSource: 'newsletter',
        };
        const current = { ...summary, visits: 1, topCampaigns: [breakdown], topSources: [breakdown], recentEvents: [event] };
        const byDay = [{ date: '2024-02-29', visits: 1, signups: 0, premiumStarts: 0 }];
        const receive = vi.fn();
        service.getRange(filters).subscribe(receive);

        const request = httpMock.expectOne(candidate => candidate.url === summaryUrl.replace(/summary$/u, 'range'));
        expect(request.request.method).toBe('GET');
        expect(Object.fromEntries(request.request.params.keys().map(key => [key, request.request.params.get(key)]))).toEqual(
            Object.fromEntries(Object.entries(filters).map(([key, value]) => [key, String(value)])),
        );
        request.flush({
            fromUtc: filters.fromUtc,
            toUtc: filters.toUtc,
            previousFromUtc: '2024-02-28T00:00:00Z',
            current,
            previous: summary,
            byDay,
            eventTotal: 51,
        });

        expect(receive).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                byDay,
                eventTotal: 51,
                current: {
                    ...current,
                    topCampaigns: [{ ...breakdown, lastEventAtUtc: null }],
                    topSources: [{ ...breakdown, lastEventAtUtc: null }],
                    recentEvents: [
                        {
                            ...event,
                            referrerHost: null,
                            utmMedium: null,
                            utmCampaign: null,
                            utmContent: null,
                            utmTerm: null,
                            buildVersion: null,
                        },
                    ],
                },
            }),
        );
    });
});
