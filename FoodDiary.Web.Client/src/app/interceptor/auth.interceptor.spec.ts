import {
    HTTP_INTERCEPTORS,
    HttpClient,
    HttpContext,
    HttpErrorResponse,
    provideHttpClient,
    withInterceptorsFromDi,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { SKIP_AUTH } from '../constants/http-context.tokens';
import { AuthService } from '../services/auth.service';
import { AuthInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let authServiceSpy: any;

    beforeEach(() => {
        authServiceSpy = { getToken: vi.fn(), refreshToken: vi.fn(), onLogout: vi.fn() } as any;
        authServiceSpy.onLogout.mockReturnValue(Promise.resolve());

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
                { provide: AuthService, useValue: authServiceSpy },
            ],
        });

        http = TestBed.inject(HttpClient);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpTesting.verify();
    });

    it('should add Authorization header when token exists', () => {
        authServiceSpy.getToken.mockReturnValue('test-token');

        http.get('/api/data').subscribe();

        const req = httpTesting.expectOne('/api/data');
        expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
        req.flush({});
    });

    it('should not add Authorization header when no token', () => {
        authServiceSpy.getToken.mockReturnValue(null);

        http.get('/api/data').subscribe();

        const req = httpTesting.expectOne('/api/data');
        expect(req.request.headers.has('Authorization')).toBe(false);
        req.flush({});
    });

    it('should skip auth header when SKIP_AUTH context is true', () => {
        authServiceSpy.getToken.mockReturnValue('test-token');

        http.get('/api/upload', { context: new HttpContext().set(SKIP_AUTH, true) }).subscribe();

        const req = httpTesting.expectOne('/api/upload');
        expect(req.request.headers.has('Authorization')).toBe(false);
        req.flush({});
    });

    it('should pass through request on success', () => {
        authServiceSpy.getToken.mockReturnValue('test-token');
        const responseData = { id: 1, name: 'Test' };

        http.get<{ id: number; name: string }>('/api/data').subscribe(data => {
            expect(data).toEqual(responseData);
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(responseData);
    });

    it('should attempt token refresh on 401 error', () => {
        authServiceSpy.getToken.mockReturnValue('expired-token');
        authServiceSpy.refreshToken.mockReturnValue(of('new-token'));

        http.get('/api/data').subscribe();

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.refreshToken).toHaveBeenCalledTimes(1);

        const retryReq = httpTesting.expectOne('/api/data');
        expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-token');
        retryReq.flush({});
    });

    it('should retry original request with new token after successful refresh', () => {
        authServiceSpy.getToken.mockReturnValue('expired-token');
        authServiceSpy.refreshToken.mockReturnValue(of('refreshed-token'));
        const responseData = { success: true };

        http.get<{ success: boolean }>('/api/data').subscribe(data => {
            expect(data).toEqual(responseData);
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        const retryReq = httpTesting.expectOne('/api/data');
        expect(retryReq.request.headers.get('Authorization')).toBe('Bearer refreshed-token');
        retryReq.flush(responseData);
    });

    it('should logout on refresh failure', () => {
        authServiceSpy.getToken.mockReturnValue('expired-token');
        authServiceSpy.refreshToken.mockReturnValue(throwError(() => new Error('Refresh failed')));

        http.get('/api/data').subscribe({
            error: (error: Error) => {
                expect(error.message).toBe('Refresh failed');
            },
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.onLogout).toHaveBeenCalledWith(true);
    });

    it('should logout when refresh returns null token', () => {
        authServiceSpy.getToken.mockReturnValue('expired-token');
        authServiceSpy.refreshToken.mockReturnValue(of(null));

        http.get('/api/data').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(401);
            },
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.onLogout).toHaveBeenCalledWith(true);
    });

    it('should not refresh for auth requests (URL contains /auth/)', () => {
        authServiceSpy.getToken.mockReturnValue('some-token');

        http.get('/api/auth/login').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(401);
            },
        });

        const req = httpTesting.expectOne('/api/auth/login');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.refreshToken).not.toHaveBeenCalled();
        expect(authServiceSpy.onLogout).not.toHaveBeenCalled();
    });

    it('should propagate non-401 errors without refresh attempt', () => {
        authServiceSpy.getToken.mockReturnValue('test-token');

        http.get('/api/data').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(500);
            },
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 500, statusText: 'Internal Server Error' });

        expect(authServiceSpy.refreshToken).not.toHaveBeenCalled();
        expect(authServiceSpy.onLogout).not.toHaveBeenCalled();
    });
});
