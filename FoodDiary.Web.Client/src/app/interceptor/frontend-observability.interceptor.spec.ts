import {
    HTTP_INTERCEPTORS,
    HttpClient,
    HttpContext,
    type HttpInterceptor,
    HttpStatusCode,
    provideHttpClient,
    withInterceptorsFromDi,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { SKIP_OBSERVABILITY } from '../constants/observability-context.tokens';
import { FrontendObservabilityService } from '../services/frontend-observability.service';
import { developmentRequestContext } from '../shared/development/development-request-context';
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
    registerBoundaryTests(() => ({ http, httpTesting, observabilitySpy, completeDevelopmentRequest, observeDevelopmentRequest }));
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

type InterceptorContext = {
    http: HttpClient;
    httpTesting: HttpTestingController;
    observabilitySpy: { recordHttpRequest: ReturnType<typeof vi.fn> };
    completeDevelopmentRequest: ReturnType<typeof vi.fn>;
    observeDevelopmentRequest: ReturnType<typeof vi.fn>;
};

function registerBoundaryTests(context: () => InterceptorContext): void {
    it('binds tracked requests and forwards only API identity metadata for a client error', () => {
        const { http, httpTesting, observabilitySpy, completeDevelopmentRequest } = context();
        const requestId = 17;
        Object.assign(completeDevelopmentRequest, { requestId });
        const bind = vi.fn();
        const receiveError = vi.fn();

        http.get('/api/v1/products', { context: developmentRequestContext(bind) }).subscribe({ error: receiveError });

        const request = httpTesting.expectOne('/api/v1/products');
        expect(bind).toHaveBeenCalledExactlyOnceWith(requestId);
        request.flush(null, {
            status: HttpStatusCode.Conflict,
            statusText: 'Conflict',
            headers: { 'X-FoodDiary-Api-Build': 'synthetic-build', 'X-Unrelated-Trace': 'local-sample' },
        });

        expect(completeDevelopmentRequest).toHaveBeenCalledExactlyOnceWith(HttpStatusCode.Conflict, {
            apiBuild: 'synthetic-build',
            apiInstance: null,
        });
        expect(receiveError).toHaveBeenCalledWith(expect.objectContaining({ status: HttpStatusCode.Conflict }));
        expect(observabilitySpy.recordHttpRequest).toHaveBeenCalledWith(
            expect.objectContaining({ statusCode: HttpStatusCode.Conflict, outcome: 'client_error' }),
        );
    });

    it('records browser network failures without response metadata', () => {
        const { http, httpTesting, observabilitySpy, completeDevelopmentRequest } = context();
        const receiveError = vi.fn();
        http.get('/api/v1/products').subscribe({ error: receiveError });
        httpTesting.expectOne('/api/v1/products').error(new ProgressEvent('error'));

        expect(completeDevelopmentRequest).toHaveBeenCalledExactlyOnceWith(0);
        expect(receiveError).toHaveBeenCalledWith(expect.objectContaining({ status: 0 }));
        expect(observabilitySpy.recordHttpRequest).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ statusCode: 0, outcome: 'network_error' }),
        );
    });

    it('excludes explicitly skipped requests and static resources from all observation', () => {
        const { http, httpTesting, observabilitySpy, observeDevelopmentRequest } = context();
        const skip = new HttpContext().set(SKIP_OBSERVABILITY, true);

        http.get('/api/v1/products', { context: skip }).subscribe();
        http.get('/assets/theme.json').subscribe();
        httpTesting.expectOne('/api/v1/products').flush({});
        httpTesting.expectOne('/assets/theme.json').flush({});

        expect(observabilitySpy.recordHttpRequest).not.toHaveBeenCalled();
        expect(observeDevelopmentRequest).not.toHaveBeenCalled();
    });
}

it('records a downstream client failure before a request reaches the HTTP backend', async () => {
    const failure = new TypeError('Synthetic request transform failed');
    const downstream: HttpInterceptor = { intercept: () => throwError(() => failure) };
    const recordHttpRequest = vi.fn();
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
            { provide: HTTP_INTERCEPTORS, useClass: FrontendObservabilityInterceptor, multi: true },
            { provide: HTTP_INTERCEPTORS, useValue: downstream, multi: true },
            { provide: FrontendObservabilityService, useValue: { recordHttpRequest } },
        ],
    });

    await expect(firstValueFrom(TestBed.inject(HttpClient).get('/api/v1/products'))).rejects.toBe(failure);

    const httpTesting = TestBed.inject(HttpTestingController);
    httpTesting.expectNone('/api/v1/products');
    httpTesting.verify();
    expect(recordHttpRequest).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ statusCode: 0, outcome: 'network_error' }));
});
