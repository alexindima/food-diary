import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { environment } from '../../environments/environment';
import { SKIP_AUTH } from '../constants/http-context.tokens';
import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';
import { type ClientTelemetryEvent, LoggingApiService } from './logging-api.service';

const EVENT: ClientTelemetryEvent = {
    category: 'user_action',
    name: 'clicked',
    level: 'info',
    timestamp: '2026-01-01T00:00:00.000Z',
};
const MAX_KEEPALIVE_BYTES = 65_536;
const LONG_DIAGNOSTIC_LENGTH = 100_000;
const API_MESSAGE_LIMIT = 512;
const API_STACK_LIMIT = 1024;
const API_DETAILS_LIMIT = 4096;

describe('LoggingApiService payload bounds', () => {
    it('bounds oversized multibyte and cyclic diagnostics below the keepalive body limit', () => {
        const { service, httpMock } = setup();
        const details: Record<string, unknown> = { huge: 'я'.repeat(LONG_DIAGNOSTIC_LENGTH) };
        details['cycle'] = details;
        service
            .logEvent({ ...EVENT, message: 'я'.repeat(LONG_DIAGNOSTIC_LENGTH), stack: 'я'.repeat(LONG_DIAGNOSTIC_LENGTH), details })
            .subscribe();
        const request = httpMock.expectOne(environment.apiUrls.logs);
        expect(new TextEncoder().encode(JSON.stringify(request.request.body)).byteLength).toBeLessThan(MAX_KEEPALIVE_BYTES);
        const body = request.request.body as ClientTelemetryEvent;
        expect(body.message?.length, 'Telemetry payload must satisfy the API field limits').toBeLessThanOrEqual(API_MESSAGE_LIMIT);
        expect(body.stack?.length).toBeLessThanOrEqual(API_STACK_LIMIT);
        expect(JSON.stringify(body.details).length).toBeLessThanOrEqual(API_DETAILS_LIMIT);
        expect(request.request.body).toMatchObject({ category: EVENT.category, name: EVENT.name });
        request.flush(null);
        httpMock.verify();
    });
});

describe('LoggingApiService', () => {
    it('masks personal data and credential-bearing text before it reaches the SDK transport', () => {
        const { service, httpMock } = setup();
        service
            .logEvent({
                ...EVENT,
                message: 'failed for person@example.test at https://fooddiary.test/profile?code=secret',
                stack: 'Bearer secret-access-token',
                details: {
                    token: 'private-credential',
                    nested: { email: 'person@example.test', url: 'https://fooddiary.test/path?password=hidden' },
                },
            })
            .subscribe();
        const req = httpMock.expectOne(environment.apiUrls.logs);
        expect(JSON.stringify(req.request.body), 'Telemetry transport must mask personal data and credentials').not.toMatch(
            /person@|secret|private-credential|hidden/u,
        );
        req.flush(null);
        httpMock.verify();
    });
    it('posts telemetry events without auth and observability interceptors', () => {
        const { service, httpMock } = setup();

        service.logEvent(EVENT).subscribe();

        const req = httpMock.expectOne(environment.apiUrls.logs);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(EVENT);
        expect(req.request.context.get(SKIP_AUTH)).toBe(true);
        expect(req.request.context.get(SKIP_OBSERVABILITY)).toBe(true);
        expect(req.request.withCredentials).toBe(false);
        expect(req.request.keepalive, 'Page-exit telemetry must use keepalive').toBe(true);
        expect(req.request.priority).toBe('low');
        req.flush(null);
        httpMock.verify();
    });

    it('logs errors through the same telemetry endpoint', () => {
        const { service, httpMock } = setup();

        service.logError(EVENT).subscribe();

        const req = httpMock.expectOne(environment.apiUrls.logs);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(EVENT);
        req.flush(null);
        httpMock.verify();
    });
});

function setup(): { service: LoggingApiService; httpMock: HttpTestingController } {
    TestBed.configureTestingModule({
        providers: [LoggingApiService, provideHttpClient(), provideHttpClientTesting()],
    });

    return {
        service: TestBed.inject(LoggingApiService),
        httpMock: TestBed.inject(HttpTestingController),
    };
}
