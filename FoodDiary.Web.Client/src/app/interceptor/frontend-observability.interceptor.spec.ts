import { HTTP_INTERCEPTORS, HttpClient, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../services/frontend-observability.service';
import { FrontendObservabilityInterceptor } from './frontend-observability.interceptor';

describe('FrontendObservabilityInterceptor', () => {
    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let observabilitySpy: { recordHttpRequest: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        observabilitySpy = {
            recordHttpRequest: vi.fn(),
        };

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                { provide: HTTP_INTERCEPTORS, useClass: FrontendObservabilityInterceptor, multi: true },
                { provide: FrontendObservabilityService, useValue: observabilitySpy },
            ],
        });

        http = TestBed.inject(HttpClient);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpTesting.verify();
    });

    it('should record successful api requests', () => {
        http.get('/api/v1/products').subscribe();

        const req = httpTesting.expectOne('/api/v1/products');
        req.flush({});

        expect(observabilitySpy.recordHttpRequest).toHaveBeenCalledTimes(1);
        expect(observabilitySpy.recordHttpRequest).toHaveBeenCalledWith(
            expect.objectContaining({
                method: 'GET',
                statusCode: 200,
                outcome: 'success',
            }),
        );
    });

    it('should record failed api requests', () => {
        http.get('/api/v1/products').subscribe({
            error: () => {
                // expected
            },
        });

        const req = httpTesting.expectOne('/api/v1/products');
        req.flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });

        expect(observabilitySpy.recordHttpRequest).toHaveBeenCalledWith(
            expect.objectContaining({
                method: 'GET',
                statusCode: 500,
                outcome: 'server_error',
            }),
        );
    });

    it('should skip telemetry requests themselves', () => {
        http.post('/api/v1/logs', {}).subscribe();

        const req = httpTesting.expectOne('/api/v1/logs');
        req.flush({});

        expect(observabilitySpy.recordHttpRequest).not.toHaveBeenCalled();
    });
});
