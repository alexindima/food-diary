import { HTTP_INTERCEPTORS, HttpClient, HttpStatusCode, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { getNumberProperty } from '../shared/lib/unknown-value.utils';
import { RetryInterceptor } from './retry.interceptor';

describe('RetryInterceptor', () => {
    let http: HttpClient;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: HTTP_INTERCEPTORS,
                    useClass: RetryInterceptor,
                    multi: true,
                },
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
            ],
        });

        http = TestBed.inject(HttpClient);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should pass through successful requests', () => {
        http.get('/api/test').subscribe(response => {
            expect(response).toEqual({ data: 'ok' });
        });

        const req = httpMock.expectOne('/api/test');
        req.flush({ data: 'ok' });
    });

    it('should not retry on 400 errors', () => {
        http.get('/api/test').subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.BadRequest);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Bad Request', { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
    });

    it('should not retry on 404 errors', () => {
        http.get('/api/test').subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.NotFound);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Not Found', { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    });

    it('should not retry POST requests', () => {
        http.post('/api/test', { value: 1 }).subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});
