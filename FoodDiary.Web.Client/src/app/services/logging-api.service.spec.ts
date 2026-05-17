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

describe('LoggingApiService', () => {
    it('posts telemetry events without auth and observability interceptors', () => {
        const { service, httpMock } = setup();

        service.logEvent(EVENT).subscribe();

        const req = httpMock.expectOne(environment.apiUrls.logs);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(EVENT);
        expect(req.request.context.get(SKIP_AUTH)).toBe(true);
        expect(req.request.context.get(SKIP_OBSERVABILITY)).toBe(true);
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
