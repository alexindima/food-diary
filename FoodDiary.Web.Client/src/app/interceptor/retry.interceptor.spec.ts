import { HTTP_INTERCEPTORS, HttpClient, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { RETRY_TIMING_CONFIG } from '../config/runtime-ui.tokens';
import { getNumberProperty } from '../shared/lib/unknown-value.utils';
import { RetryInterceptor } from './retry.interceptor';

const BASE_DELAY_MS = 100;
const SECOND_DELAY_MS = 200;

let http: HttpClient;
let httpMock: HttpTestingController;

beforeEach(() => {
    vi.useFakeTimers();

    TestBed.configureTestingModule({
        providers: [
            {
                provide: HTTP_INTERCEPTORS,
                useClass: RetryInterceptor,
                multi: true,
            },
            { provide: RETRY_TIMING_CONFIG, useValue: { attemptCount: 2, baseDelayMs: BASE_DELAY_MS } },
            provideHttpClient(withInterceptorsFromDi()),
            provideHttpClientTesting(),
        ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    vi.useRealTimers();
    httpMock.verify();
});

describe('RetryInterceptor request selection', () => {
    it('should pass through successful requests', () => {
        const onNext = vi.fn();
        http.get('/api/test').subscribe(onNext);

        const req = httpMock.expectOne('/api/test');
        req.flush({ data: 'ok' });

        expect(onNext).toHaveBeenCalledExactlyOnceWith({ data: 'ok' });
    });

    it('should not retry on 400 errors', () => {
        const onError = vi.fn();
        http.get('/api/test').subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.BadRequest);
                onError(error);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Bad Request', { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });

        expect(onError).toHaveBeenCalledOnce();
        vi.runAllTimers();
        httpMock.expectNone('/api/test');
    });

    it('should not retry on 404 errors', () => {
        const onError = vi.fn();
        http.get('/api/test').subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.NotFound);
                onError(error);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Not Found', { status: HttpStatusCode.NotFound, statusText: 'Not Found' });

        expect(onError).toHaveBeenCalledOnce();
        vi.runAllTimers();
        httpMock.expectNone('/api/test');
    });

    it('should not retry POST requests', () => {
        const onError = vi.fn();
        http.post('/api/test', { value: 1 }).subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
                onError(error);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });

        expect(onError).toHaveBeenCalledOnce();
        vi.runAllTimers();
        httpMock.expectNone('/api/test');
    });
});

describe('RetryInterceptor recovery timing', () => {
    it('retries GET server failures after exponentially increasing configured delays', () => {
        const onNext = vi.fn();
        const onError = vi.fn();
        http.get('/api/test').subscribe({ next: onNext, error: onError });

        httpMock.expectOne('/api/test').flush('Unavailable', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });

        vi.advanceTimersByTime(BASE_DELAY_MS - 1);
        httpMock.expectNone('/api/test');
        expect(onError).not.toHaveBeenCalled();
        expect(onNext).not.toHaveBeenCalled();

        vi.advanceTimersByTime(1);
        httpMock.expectOne('/api/test').flush('Still unavailable', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });

        vi.advanceTimersByTime(SECOND_DELAY_MS - 1);
        httpMock.expectNone('/api/test');
        expect(onError).not.toHaveBeenCalled();
        expect(onNext).not.toHaveBeenCalled();

        vi.advanceTimersByTime(1);
        httpMock.expectOne('/api/test').flush({ data: 'recovered' });

        expect(onNext).toHaveBeenCalledExactlyOnceWith({ data: 'recovered' });
        expect(onError).not.toHaveBeenCalled();
        vi.runAllTimers();
        httpMock.expectNone('/api/test');
    });

    it('retries a GET network failure and delivers the recovered response', () => {
        const onNext = vi.fn();
        const onError = vi.fn();
        http.get('/api/test').subscribe({ next: onNext, error: onError });

        httpMock.expectOne('/api/test').error(new ProgressEvent('offline'));

        vi.advanceTimersByTime(BASE_DELAY_MS - 1);
        httpMock.expectNone('/api/test');
        expect(onError).not.toHaveBeenCalled();

        vi.advanceTimersByTime(1);
        httpMock.expectOne('/api/test').flush({ data: 'online' });

        expect(onNext).toHaveBeenCalledExactlyOnceWith({ data: 'online' });
        expect(onError).not.toHaveBeenCalled();
    });
});

describe('RetryInterceptor retry limits', () => {
    it('reports the last GET failure after exactly the configured number of retries', () => {
        const onNext = vi.fn();
        const onError = vi.fn();
        http.get('/api/test').subscribe({ next: onNext, error: onError });

        httpMock.expectOne('/api/test').flush('Initial failure', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });

        vi.advanceTimersByTime(BASE_DELAY_MS);
        httpMock.expectOne('/api/test').flush('First retry failure', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });
        expect(onError).not.toHaveBeenCalled();

        vi.advanceTimersByTime(SECOND_DELAY_MS);
        httpMock.expectOne('/api/test').flush('Final retry failure', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });

        expect(onError).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ status: HttpStatusCode.ServiceUnavailable, error: 'Final retry failure' }),
        );
        expect(onNext).not.toHaveBeenCalled();
        vi.runAllTimers();
        httpMock.expectNone('/api/test');
    });

    it('cancels a pending retry when the consumer unsubscribes', () => {
        const onNext = vi.fn();
        const onError = vi.fn();
        const onComplete = vi.fn();
        const subscription = http.get('/api/test').subscribe({ next: onNext, error: onError, complete: onComplete });

        httpMock.expectOne('/api/test').flush('Unavailable', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });

        vi.advanceTimersByTime(BASE_DELAY_MS - 1);
        subscription.unsubscribe();
        vi.runAllTimers();

        httpMock.expectNone('/api/test');
        expect(onNext).not.toHaveBeenCalled();
        expect(onError).not.toHaveBeenCalled();
        expect(onComplete).not.toHaveBeenCalled();
    });
});
