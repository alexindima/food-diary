import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { WearableConnection, WearableDailySummary } from '../models/wearable.data';
import { WearableService } from './wearable.service';

const BASE_URL = environment.apiUrls.wearables;
const CONNECTION: WearableConnection = {
    provider: 'Fitbit',
    externalUserId: 'fitbit-user',
    isActive: true,
    lastSyncedAtUtc: '2026-05-15T00:00:00Z',
    connectedAtUtc: '2026-05-01T00:00:00Z',
};
const SUMMARY: WearableDailySummary = {
    date: '2026-05-15',
    steps: 12_345,
    heartRate: 62,
    caloriesBurned: 450,
    activeMinutes: 54,
    sleepMinutes: 455,
};

let service: WearableService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [WearableService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(WearableService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('WearableService connections', () => {
    it('gets connections', () => {
        service.getConnections().subscribe(connections => {
            expect(connections).toEqual([CONNECTION]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/connections`);
        expect(req.request.method).toBe('GET');
        req.flush([CONNECTION]);
    });

    it('returns empty connections on error', () => {
        service.getConnections().subscribe(connections => {
            expect(connections).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/connections`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('gets auth url with state query param', () => {
        service.getAuthUrl('Fitbit', 'state-1').subscribe(result => {
            expect(result).toEqual({ authorizationUrl: 'https://example.com/oauth' });
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/Fitbit/auth-url`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('state')).toBe('state-1');
        req.flush({ authorizationUrl: 'https://example.com/oauth' });
    });

    it('connects and disconnects provider', () => {
        service.connect('Fitbit', 'code-1').subscribe(connection => {
            expect(connection).toEqual(CONNECTION);
        });

        const connectReq = httpMock.expectOne(`${BASE_URL}/Fitbit/connect`);
        expect(connectReq.request.method).toBe('POST');
        expect(connectReq.request.body).toEqual({ code: 'code-1' });
        connectReq.flush(CONNECTION);

        service.disconnect('Fitbit').subscribe();

        const disconnectReq = httpMock.expectOne(`${BASE_URL}/Fitbit/disconnect`);
        expect(disconnectReq.request.method).toBe('DELETE');
        disconnectReq.flush(null);
    });
});

describe('WearableService daily summary', () => {
    it('syncs provider data for date', () => {
        service.sync('Fitbit', '2026-05-15').subscribe(summary => {
            expect(summary).toEqual(SUMMARY);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/Fitbit/sync`);
        expect(req.request.method).toBe('POST');
        expect(req.request.params.get('date')).toBe('2026-05-15');
        req.flush(SUMMARY);
    });

    it('gets daily summary with date query param', () => {
        service.getDailySummary('2026-05-15').subscribe(summary => {
            expect(summary).toEqual(SUMMARY);
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/daily-summary`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('date')).toBe('2026-05-15');
        req.flush(SUMMARY);
    });

    it('returns empty daily summary on error', () => {
        service.getDailySummary('2026-05-15').subscribe(summary => {
            expect(summary).toEqual({
                date: '2026-05-15',
                steps: null,
                heartRate: null,
                caloriesBurned: null,
                activeMinutes: null,
                sleepMinutes: null,
            });
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/daily-summary`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});
