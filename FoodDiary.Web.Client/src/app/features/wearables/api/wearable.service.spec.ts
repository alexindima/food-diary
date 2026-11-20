import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { WearableConnectionHttpResponse } from '../../../shared/api/sdk/generated/model/wearable-connection-http-response';
import type { WearableDailySummaryHttpResponse } from '../../../shared/api/sdk/generated/model/wearable-daily-summary-http-response';
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
        service.connect('Fitbit', 'code-1', 'state-1').subscribe(connection => {
            expect(connection).toEqual(CONNECTION);
        });

        const connectReq = httpMock.expectOne(`${BASE_URL}/Fitbit/connect`);
        expect(connectReq.request.method).toBe('POST');
        expect(connectReq.request.body).toEqual({ code: 'code-1', state: 'state-1' });
        expect(connectReq.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
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
        expect(req.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
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

describe('WearableService provider failures', () => {
    it('propagates the unavailable provider setup instead of returning an authorization URL', async () => {
        const pending = firstValueFrom(service.getAuthUrl('Fitbit', 'state-1'));
        const body = { error: 'Wearable.ProviderNotConfigured' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne(candidate => candidate.url === `${BASE_URL}/Fitbit/auth-url`);
        expect(request.request.method).toBe('GET');
        expect(request.request.params.get('state')).toBe('state-1');
        request.flush(body, { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.InternalServerError, error: body });
    });

    it('passes an expired provider callback state back to the caller', async () => {
        const pending = firstValueFrom(service.connect('Fitbit', 'authorization-code', 'expired-state'));
        const body = { error: 'Wearable.InvalidState' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: `${BASE_URL}/Fitbit/connect` });
        expect(request.request.body).toEqual({ code: 'authorization-code', state: 'expired-state' });
        request.flush(body, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.Unauthorized, error: body });
    });

    it('propagates an already missing connection during disconnect', async () => {
        const pending = firstValueFrom(service.disconnect('Fitbit'));
        const body = { error: 'Wearable.NotConnected' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'DELETE', url: `${BASE_URL}/Fitbit/disconnect` });
        expect(request.request.body).toBeNull();
        request.flush(body, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: body });
    });

    it('allows sync to be retried after a provider failure without changing the requested day', async () => {
        const requestedDate = '2026-03-08';
        const syncUrl = `${BASE_URL}/Fitbit/sync`;
        const pending = firstValueFrom(service.sync('Fitbit', requestedDate));
        const failed = httpMock.expectOne(candidate => candidate.method === 'POST' && candidate.url === syncUrl);
        const body = { error: 'Wearable.SyncFailed' } satisfies ApiErrorHttpResponse;
        expect(failed.request.params.get('date')).toBe(requestedDate);
        const idempotencyKey = failed.request.headers.get('Idempotency-Key');
        expect(idempotencyKey).toMatch(/^[0-9a-f-]{36}$/u);
        failed.flush(body, { status: HttpStatusCode.BadGateway, statusText: 'Bad Gateway' });
        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.BadGateway, error: body });

        const retry = firstValueFrom(service.sync('Fitbit', requestedDate));
        const request = httpMock.expectOne(candidate => candidate.method === 'POST' && candidate.url === syncUrl);
        expect(request.request.params.get('date')).toBe(requestedDate);
        expect(request.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        expect(request.request.headers.get('Idempotency-Key')).not.toBe(idempotencyKey);
        const payload = { date: requestedDate } satisfies WearableDailySummaryHttpResponse;
        request.flush(payload);
        await expect(retry).resolves.toEqual({
            date: requestedDate,
            steps: null,
            heartRate: null,
            caloriesBurned: null,
            activeMinutes: null,
            sleepMinutes: null,
        });
    });
});

describe('WearableService partial SDK responses', () => {
    it('recognizes a new connection before its first sync', async () => {
        const pending = firstValueFrom(service.getConnections());
        const newConnection = {
            provider: CONNECTION.provider,
            externalUserId: CONNECTION.externalUserId,
            connectedAtUtc: CONNECTION.connectedAtUtc,
            isActive: true,
        } satisfies WearableConnectionHttpResponse;
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/connections` }).flush([newConnection]);

        await expect(pending).resolves.toEqual([{ ...newConnection, lastSyncedAtUtc: null }]);
    });

    it('keeps recorded zero activity separate from unavailable device metrics', async () => {
        const pending = firstValueFrom(service.getDailySummary(SUMMARY.date));
        const request = httpMock.expectOne(candidate => candidate.method === 'GET' && candidate.url === `${BASE_URL}/daily-summary`);
        expect(request.request.params.get('date')).toBe(SUMMARY.date);
        const payload = { date: SUMMARY.date, steps: 0, activeMinutes: 0 } satisfies WearableDailySummaryHttpResponse;
        request.flush(payload);

        await expect(pending).resolves.toEqual({ ...payload, heartRate: null, caloriesBurned: null, sleepMinutes: null });
    });
});
