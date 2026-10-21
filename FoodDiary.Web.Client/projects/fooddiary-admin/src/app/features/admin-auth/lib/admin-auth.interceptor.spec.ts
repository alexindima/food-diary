import { HttpClient, HttpErrorResponse, HttpStatusCode, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { adminAuthInterceptor } from './admin-auth.interceptor';

const getHttpStatus = (error: unknown): number | undefined => (error instanceof HttpErrorResponse ? error.status : undefined);

describe('adminAuthInterceptor', () => {
    let http: HttpClient;
    let httpMock: HttpTestingController;
    let router: { url: string; navigate: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        localStorage.clear();
        sessionStorage.clear();

        router = {
            url: '/users?page=2',
            navigate: vi.fn(),
        };
        router.navigate.mockResolvedValue(true);

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([adminAuthInterceptor])),
                provideHttpClientTesting(),
                { provide: Router, useValue: router },
            ],
        });

        http = TestBed.inject(HttpClient);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
        localStorage.clear();
        sessionStorage.clear();
    });

    it('should add Authorization header when token exists', () => {
        localStorage.setItem('authToken', 'admin-token');

        http.get('/api/admin/users').subscribe();

        const req = httpMock.expectOne('/api/admin/users');
        expect(req.request.headers.get('Authorization')).toBe('Bearer admin-token');
        req.flush({});
    });

    it.each([null, { error: 'Ai.ConsentRequired' }])('should redirect to unauthorized and clear tokens on 401 (%j)', payload => {
        localStorage.setItem('authToken', 'admin-token');
        localStorage.setItem('refreshToken', 'refresh-token');
        sessionStorage.setItem('authToken', 'session-token');

        http.get('/api/admin/users').subscribe({
            error: (error: unknown) => {
                expect(getHttpStatus(error)).toBe(HttpStatusCode.Unauthorized);
            },
        });

        const req = httpMock.expectOne('/api/admin/users');
        req.flush(payload, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        expect(localStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
        expect(router.navigate).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl: '/users?page=2' },
        });
    });

    it.each([null, { error: 'Access.Forbidden' }, { error: 'Ai.UnknownForbidden' }])('redirects ordinary403 (%j)', payload => {
        localStorage.setItem('authToken', 'admin-token');

        http.get('/api/admin/users').subscribe({
            error: (error: unknown) => {
                expect(getHttpStatus(error)).toBe(HttpStatusCode.Forbidden);
            },
        });

        const req = httpMock.expectOne('/api/admin/users');
        req.flush(payload, { status: HttpStatusCode.Forbidden, statusText: 'Forbidden' });

        expect(localStorage.getItem('authToken')).toBe('admin-token');
        expect(router.navigate).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl: '/users?page=2' },
        });
    });

    it('should not redirect for admin sso exchange request failures', () => {
        localStorage.setItem('authToken', 'admin-token');

        http.post('/api/v1/auth/admin-sso/exchange', {}).subscribe({
            error: (error: unknown) => {
                expect(getHttpStatus(error)).toBe(HttpStatusCode.Unauthorized);
            },
        });

        const req = httpMock.expectOne('/api/v1/auth/admin-sso/exchange');
        req.flush(null, { status: HttpStatusCode.Unauthorized, statusText: 'Unauthorized' });

        expect(router.navigate).not.toHaveBeenCalled();
    });
});

describe('adminAuthInterceptor feature policy errors', () => {
    it('rethrows AI consent403 to the feature without redirecting or clearing authentication', () => {
        localStorage.setItem('authToken', 'admin-token');
        const router = { url: '/ai-prompts', navigate: vi.fn().mockResolvedValue(true) };
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([adminAuthInterceptor])),
                provideHttpClientTesting(),
                { provide: Router, useValue: router },
            ],
        });
        const http = TestBed.inject(HttpClient);
        const controller = TestBed.inject(HttpTestingController);
        const received = vi.fn();
        http.post('/api/v1/admin/ai-prompts/test', {}).subscribe({ error: received });
        const payload = { error: 'Ai.ConsentRequired', message: 'Consent required' };
        controller.expectOne('/api/v1/admin/ai-prompts/test').flush(payload, {
            status: HttpStatusCode.Forbidden,
            statusText: 'Forbidden',
        });
        expect(received).toHaveBeenCalledWith(expect.objectContaining({ status: HttpStatusCode.Forbidden, error: payload }));
        expect(router.navigate).not.toHaveBeenCalled();
        expect(localStorage.getItem('authToken')).toBe('admin-token');
        controller.verify();
        localStorage.clear();
    });
});
