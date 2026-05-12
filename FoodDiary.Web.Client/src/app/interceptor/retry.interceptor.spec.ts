import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { getNumberProperty } from '../shared/lib/unknown-value.utils';
import { RetryInterceptor } from './retry.interceptor';

const HTTP_BAD_REQUEST = 400;
const HTTP_NOT_FOUND = 404;
const HTTP_INTERNAL_SERVER_ERROR = 500;

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
                expect(getNumberProperty(error, 'status')).toBe(HTTP_BAD_REQUEST);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Bad Request', { status: HTTP_BAD_REQUEST, statusText: 'Bad Request' });
    });

    it('should not retry on 404 errors', () => {
        http.get('/api/test').subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HTTP_NOT_FOUND);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Not Found', { status: HTTP_NOT_FOUND, statusText: 'Not Found' });
    });

    it('should not retry POST requests', () => {
        http.post('/api/test', { value: 1 }).subscribe({
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HTTP_INTERNAL_SERVER_ERROR);
            },
        });

        const req = httpMock.expectOne('/api/test');
        req.flush('Server Error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });
});
