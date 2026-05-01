import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { adminAuthInterceptor } from './admin-auth.interceptor';

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
        router.navigate.mockReturnValue(Promise.resolve(true));

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

    it('should redirect to unauthorized and clear tokens on 401', () => {
        localStorage.setItem('authToken', 'admin-token');
        localStorage.setItem('refreshToken', 'refresh-token');
        sessionStorage.setItem('authToken', 'session-token');

        http.get('/api/admin/users').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(401);
            },
        });

        const req = httpMock.expectOne('/api/admin/users');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(localStorage.getItem('authToken')).toBeNull();
        expect(localStorage.getItem('refreshToken')).toBeNull();
        expect(sessionStorage.getItem('authToken')).toBeNull();
        expect(router.navigate).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'unauthenticated', returnUrl: '/users?page=2' },
        });
    });

    it('should redirect with forbidden reason on 403', () => {
        localStorage.setItem('authToken', 'admin-token');

        http.get('/api/admin/users').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(403);
            },
        });

        const req = httpMock.expectOne('/api/admin/users');
        req.flush(null, { status: 403, statusText: 'Forbidden' });

        expect(router.navigate).toHaveBeenCalledWith(['/unauthorized'], {
            queryParams: { reason: 'forbidden', returnUrl: '/users?page=2' },
        });
    });

    it('should not redirect for admin sso exchange request failures', () => {
        localStorage.setItem('authToken', 'admin-token');

        http.post('/api/v1/auth/admin-sso/exchange', {}).subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(401);
            },
        });

        const req = httpMock.expectOne('/api/v1/auth/admin-sso/exchange');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(router.navigate).not.toHaveBeenCalled();
    });
});
