import { HTTP_INTERCEPTORS, HttpClient, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../services/frontend-observability.service';
import { DEVELOPMENT_REQUESTS } from '../shared/development/development-tools';
import { FrontendObservabilityInterceptor } from './frontend-observability.interceptor';

describe('FrontendObservabilityInterceptor', () => {
    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let observabilitySpy: { recordHttpRequest: ReturnType<typeof vi.fn> };
    let completeDevelopmentRequest: ReturnType<typeof vi.fn>;
    let observeDevelopmentRequest: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        observabilitySpy = {
            recordHttpRequest: vi.fn(),
        };
        completeDevelopmentRequest = vi.fn();
        observeDevelopmentRequest = vi.fn().mockReturnValue(completeDevelopmentRequest);

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                { provide: HTTP_INTERCEPTORS, useClass: FrontendObservabilityInterceptor, multi: true },
                { provide: FrontendObservabilityService, useValue: observabilitySpy },
                { provide: DEVELOPMENT_REQUESTS, useValue: { observe: observeDevelopmentRequest } },
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
        expect(completeDevelopmentRequest).toHaveBeenCalledWith(HttpStatusCode.Ok);
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
        expect(completeDevelopmentRequest).toHaveBeenCalledWith(HttpStatusCode.InternalServerError);

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
        expect(observeDevelopmentRequest).not.toHaveBeenCalled();
    });

    it('completes optional development tracking when a request is cancelled', () => {
        const subscription = http.get('/api/v1/products').subscribe();
        const request = httpTesting.expectOne('/api/v1/products');
        subscription.unsubscribe();
        expect(request.cancelled).toBe(true);
        expect(completeDevelopmentRequest).toHaveBeenCalledOnce();
        expect(completeDevelopmentRequest).toHaveBeenCalledWith(undefined);
    });

    registerIdentityTest(() => ({ http, httpTesting, completeDevelopmentRequest }));
});

function registerIdentityTest(
    context: () => { http: HttpClient; httpTesting: HttpTestingController; completeDevelopmentRequest: ReturnType<typeof vi.fn> },
): void {
    it('passes only the dedicated API identity headers to optional development tracking', () => {
        const { http, httpTesting, completeDevelopmentRequest } = context();
        const fingerprintLength = 64;
        http.get('/api/v1/cycles/current').subscribe();
        httpTesting.expectOne('/api/v1/cycles/current').flush(
            {},
            {
                headers: {
                    'X-FoodDiary-Api-Build': 'a'.repeat(fingerprintLength),
                    'X-FoodDiary-Api-Instance': '59050212-47cf-4c78-903b-882809d3ef19',
                    Authorization: 'private-header',
                },
            },
        );
        expect(completeDevelopmentRequest).toHaveBeenCalledWith(HttpStatusCode.Ok, {
            apiBuild: 'a'.repeat(fingerprintLength),
            apiInstance: '59050212-47cf-4c78-903b-882809d3ef19',
        });
    });
}
